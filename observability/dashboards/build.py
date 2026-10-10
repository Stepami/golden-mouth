#!/usr/bin/env python3
"""Собирает тела дашбордов OpenObserve из SQL-файлов каталога panel-sql.

Запуск: python3 -I observability/dashboards/build.py
Результат: product.json и load-and-llm.json рядом со скриптом. Это готовые
аргументы (org_id + request_body) инструмента MCP CreateDashboard: их можно
отдать в OpenObserve как есть, без ручной сборки панелей.

Правка панели: меняется SQL в panel-sql/ и при необходимости подпись в этом файле.
"""

import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
SQL_DIR = os.path.join(HERE, "panel-sql")


def sql(name):
    """Читает SQL панели и убирает пустые строки по краям."""
    with open(os.path.join(SQL_DIR, name), encoding="utf-8") as fh:
        return fh.read().strip()


def columns(query):
    """Псевдонимы колонок верхнего SELECT: ими заполняются fields.x и fields.y."""
    text = " ".join(query.split())
    upper = text.upper()
    start = upper.find("SELECT ")
    if start < 0:
        return []
    start += len("SELECT ")

    depth = 0
    end = start
    while end < len(text):
        char = text[end]
        if char == "'":
            end += 1
            while end < len(text) and text[end] != "'":
                end += 1
        elif char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
        elif depth == 0 and upper.startswith("FROM ", end):
            break
        end += 1

    body = text[start:end]
    parts, depth, current = [], 0, ""
    for char in body:
        if char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
        if char == "," and depth == 0:
            parts.append(current)
            current = ""
        else:
            current += char
    if current.strip():
        parts.append(current)

    names = []
    for part in parts:
        part = part.strip()
        if not part:
            continue
        low = part.lower()
        alias = part[low.rindex(" as ") + 4:] if " as " in low else part.split()[-1]
        names.append(alias.strip().strip('"'))
    return names


def axis(name):
    return {"label": name, "alias": name, "column": name, "type": "build"}


def fields_for(query, stream, stream_type="logs", breakdown=None, values=None):
    names = columns(query)
    head, tail = (names[:1] or [""]), (names[1:] or [""])
    return {
        "stream": stream,
        "stream_type": stream_type,
        "x": [axis(n) for n in head],
        "y": [axis(n) for n in (values or [n for n in tail if n != breakdown])],
        "z": [],
        "breakdown": [axis(breakdown)] if breakdown else [],
        "filter": {"filterType": "group", "logicalOperator": "AND", "conditions": []},
    }


def sql_panel(panel_id, title, description, query, stream, layout, kind="line",
              stream_type="logs", breakdown=None, values=None):
    return {
        "id": panel_id,
        "type": kind,
        "title": title,
        "description": description,
        "queryType": "sql",
        "config": {"show_legends": kind not in ("table", "metric")},
        "queries": [{
            "query": query,
            "customQuery": True,
            "vrlFunctionQuery": "",
            "fields": fields_for(query, stream, stream_type, breakdown, values),
            "config": {"query_label": "A", "promql_legend": ""},
        }],
        "layout": layout,
    }


def promql_panel(panel_id, title, description, query, legend, layout, kind="line"):
    return {
        "id": panel_id,
        "type": kind,
        "title": title,
        "description": description,
        "queryType": "promql",
        "config": {"show_legends": True},
        "queries": [{
            "query": query,
            "customQuery": True,
            "vrlFunctionQuery": "",
            "fields": {"stream": "", "stream_type": "metrics", "x": [], "y": [],
                       "filter": {"filterType": "group", "logicalOperator": "AND", "conditions": []}},
            "config": {
                "query_label": "A",
                "promql_legend": legend,
                "step_value": "60s",
            },
        }],
        "layout": layout,
    }


def box(x, y, w, h, i):
    return {"x": x, "y": y, "w": w, "h": h, "i": i}


# --- Продуктовый дашборд -----------------------------------------------------
product_panels = [
    sql_panel(
        "p1", "DAU по дням",
        "Уникальные игроки за календарный день. activity_kind = real отсекает служебные события.",
        sql("product-p1-dau.sql"), "product", box(0, 0, 96, 8, 1)),
    sql_panel(
        "p2", "Новые игроки",
        "Первый день, в который игрок вообще появился в событиях.",
        sql("product-p2-new.sql"), "product", box(96, 0, 96, 8, 2), kind="bar"),
    sql_panel(
        "p3", "Сессии и события",
        "Число сессий и общее число событий за день. Помогает понять, растёт ли глубина, а не только охват.",
        sql("product-p3-sessions.sql"), "product", box(0, 8, 96, 8, 3)),
    sql_panel(
        "p4", "Возврат D1",
        "Сколько игроков из дня вернулись на следующий день. Знаменатель это DAU того дня.",
        sql("product-p4-retention.sql"), "product", box(96, 8, 96, 8, 4), kind="table"),
    sql_panel(
        "p9", "Churn D1",
        "Churn = 100% - возврат D1. churned это игроки дня, не пришедшие на следующий.",
        sql("product-p9-churn.sql"), "product", box(0, 16, 96, 8, 5)),
    sql_panel(
        "p5", "События и охват игроков",
        "Какие события вообще происходят и сколько уникальных игроков в каждом.",
        sql("product-p5-events.sql"), "product", box(96, 16, 96, 10, 6), kind="table"),
    sql_panel(
        "p6", "Срез по платформе",
        "Как распределены игроки и события по платформам клиента.",
        sql("product-p6-platform.sql"), "product", box(0, 26, 64, 8, 7), kind="pie", values=["players"]),
    sql_panel(
        "p7", "Срез по миру",
        "Образовательный мир, в котором играют.",
        sql("product-p7-world.sql"), "product", box(64, 26, 64, 8, 8), kind="pie", values=["players"]),
    sql_panel(
        "p8", "Срез по возрасту",
        "Возрастная группа игрока.",
        sql("product-p8-age.sql"), "product", box(128, 26, 64, 8, 9), kind="table"),
]

product = {
    "org_id": "default",
    "request_body": {
        "title": "Реки: продукт",
        "description": "DAU, новые игроки, возврат и churn D1, события и срезы. Источник: поток product.",
        "tabs": [{
            "tabId": "product",
            "name": "Продукт",
            "panels": product_panels,
        }],
    },
}

# --- Технический дашборд -----------------------------------------------------
tech_panels = [
    promql_panel(
        "t-rps", "Запросов в секунду",
        "RPS по счётчику длительности HTTP-запросов.",
        'sum(rate(http_server_request_duration_count[5m]))', "rps",
        box(0, 0, 96, 8, 1)),
    promql_panel(
        "t-p95", "Задержка p95, секунды",
        "95-й перцентиль длительности ответа сервера.",
        'histogram_quantile(0.95, sum(rate(http_server_request_duration_bucket[5m])) by (le))',
        "p95", box(96, 0, 96, 8, 2)),
    promql_panel(
        "t-5xx", "Ответы 5xx в секунду",
        "Частота серверных ошибок. Если серии 5xx отсутствуют, график не содержит данных.",
        'sum(rate(http_server_request_duration_count{http_response_status_code=~"5.."}[5m]))',
        "5xx", box(0, 8, 96, 8, 3)),
    promql_panel(
        "t-llm-calls", "Вызовов модели в секунду",
        "Частота обращений к языковой модели.",
        'sum(rate(gen_ai_client_operation_duration_count[5m]))', "вызовы модели",
        box(96, 8, 96, 8, 4)),
    promql_panel(
        "t-tokens", "Токенов в секунду",
        "Расход токенов модели. Прямо влияет на стоимость.",
        'sum(rate(gen_ai_client_token_usage_sum[5m]))', "токены",
        box(0, 16, 96, 8, 5)),
    sql_panel(
        "t2", "Ответы по кодам",
        "Распределение кодов ответа по часам. Источник: логи приложения.",
        sql("tech-t2-status.sql"), "backend", box(96, 16, 96, 8, 6), kind="bar", breakdown="statuscode"),
    sql_panel(
        "t1", "Запросы по часам",
        "Число завершённых HTTP-запросов из логов.",
        sql("tech-t1-rps.sql"), "backend", box(0, 24, 96, 8, 7)),
    sql_panel(
        "t3", "Ошибки 5xx по логам",
        "Запросы с кодом 500 и выше, разложенные по часам.",
        sql("tech-t3-5xx.sql"), "backend", box(96, 24, 96, 8, 8)),
    sql_panel(
        "t4", "Ошибки и исключения",
        "Записи уровня Error, Critical и Fatal по часам.",
        sql("tech-t4-severity.sql"), "backend", box(0, 32, 96, 8, 9), breakdown="severity"),
    sql_panel(
        "t5", "Где возникают ошибки",
        "Источник (класс) с наибольшим числом записей уровня ошибки.",
        sql("tech-t5-errors.sql"), "backend", box(96, 32, 96, 8, 10), kind="table"),
    sql_panel(
        "t6", "Операции MongoDB",
        "Вызовы базы: число, средняя и максимальная длительность в микросекундах.",
        sql("tech-t6-mongo.sql"), "backend", box(0, 40, 96, 10, 11), kind="table", stream_type="traces"),
    sql_panel(
        "t7", "Вызовы языковой модели",
        "Обращения к модели: число, средняя и максимальная длительность в микросекундах.",
        sql("tech-t7-llm.sql"), "backend", box(96, 40, 96, 8, 12), kind="table", stream_type="traces"),
]

tech = {
    "org_id": "default",
    "request_body": {
        "title": "Реки: нагрузка и LLM",
        "description": "RPS, задержка p95, ошибки, вызовы MongoDB и языковой модели. Источники: метрики, логи и трейсы бэкенда.",
        "tabs": [{
            "tabId": "load",
            "name": "Нагрузка и LLM",
            "panels": tech_panels,
        }],
    },
}

for name, body in (("product", product), ("load-and-llm", tech)):
    path = os.path.join(HERE, name + ".json")
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(body, fh, ensure_ascii=False, indent=2)
        fh.write("\n")
    print(path, "| панелей:", len(body["request_body"]["tabs"][0]["panels"]))

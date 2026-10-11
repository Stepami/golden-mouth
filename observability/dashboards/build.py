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
    # CTEs contain their own SELECT: chart fields must come from the outer query.
    depth, quoted, start = 0, False, -1
    index = 0
    while index < len(text):
        char = text[index]
        if char == "'":
            if quoted and index + 1 < len(text) and text[index + 1] == "'":
                index += 2
                continue
            quoted = not quoted
        elif not quoted:
            if char == "(":
                depth += 1
            elif char == ")":
                depth -= 1
            elif depth == 0 and upper.startswith("SELECT ", index):
                start = index + len("SELECT ")
                break
        index += 1
    if start < 0:
        return []

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
def history_panel(*args, **kwargs):
    panel = sql_panel(*args, **kwargs)
    panel["config"].update({
        "panel_time_enabled": True,
        "panel_time_range": {"type": "relative", "relativeTimePeriod": "90d"},
    })
    return panel


product_panels = [
    history_panel(
        "summary", "Исторические профили и stickiness",
        "Уникальные ID профилей в истории (90d), включая удалённые. Пересоздание профиля даёт новый ID; это не число людей. MAU30/WAU7; DAU UTC. Stickiness = DAU вчера / MAU за 30 полных дней до сегодня. История короче 30 дней → предварительная оценка.",
        sql("product-summary.sql"), "product", box(0, 0, 192, 14, 1), kind="table"),
    sql_panel(
        "p1", "Возвращается ли аудитория: DAU",
        "Крайние дни диапазона неполные. Фоновые heartbeat и отправленные уведомления не считаются посещением.",
        sql("product-p1-dau.sql"), "product", box(0, 14, 192, 12, 2), kind="line"),
    history_panel(
        "engagement", "Сколько играют и публикуют: 7 полных дней",
        "Все/миры/возраст. Посты = серверный post_scored, не клики. Время — только active_time после нового релиза; NULL до измерений. Среднее/медиана минут на измеренный активный игроко-день, measured_player_days показывает покрытие.",
        sql("product-engagement.sql"), "product", box(0, 26, 192, 28, 3), kind="table"),
    history_panel(
        "posts-world", "Интенсивность публикаций по мирам, %",
        "100% = активные игроко-дни мира за 7 полных UTC-дней. Нулевые публикации включены. Сравнивать вместе с числом игроков в таблице выше.",
        sql("product-posts-world.sql"), "product", box(0, 54, 96, 18, 4), kind="stacked", breakdown="activity_bucket", values=['player_days_pct']),
    history_panel(
        "posts-age", "Интенсивность публикаций по возрасту, %",
        "100% = активные игроко-дни возрастного диапазона за 7 полных UTC-дней. 18+ (legacy) нельзя распределить задним числом.",
        sql("product-posts-age_group.sql"), "product", box(96, 54, 96, 18, 5), kind="stacked", breakdown="activity_bucket", values=['player_days_pct']),
    history_panel(
        "stickiness", "Как часто возвращаются: DAU / MAU по сегментам",
        "Вчерашний DAU / MAU за 30 полных дней до сегодня. Не D1 retention. На короткой истории предварительно; знаменатель виден рядом.",
        sql("product-stickiness.sql"), "product", box(0, 72, 192, 26, 6), kind="table"),
    history_panel(
        "progress-bars", "Доходят ли до 1 / 3 / 10 публикаций, %",
        "Доля наблюдаемых игроков за 90d. Столбцы — отдельные пороги, не складываются. Когорты разного возраста: это охват прогресса, не причинное сравнение.",
        sql("product-progress-bars.sql"), "product", box(0, 98, 192, 20, 7), kind="bar", breakdown="milestone", values=['users_pct']),
    history_panel(
        "progress", "Темп игры: публикации и первые награды",
        "Медиана часов от первого наблюдения до третьего серверного поста только среди дошедших. Доли и база рядом; level_up = любой навык, card = любая награда. Не завершение всей игры.",
        sql("product-progress.sql"), "product", box(0, 118, 192, 28, 8), kind="table"),
    history_panel(
        "p4", "Удержание когорт: D1 / D7",
        "Возврат в точный день после первого наблюдения. Незавершённые окна исключены; NULL — ещё рано измерять.",
        sql("product-p4-retention.sql"), "product", box(0, 146, 192, 22, 9), kind="table"),
    history_panel(
        "p9", "Недельная неактивность: все и сегменты",
        "Активен в предыдущие 7 полных UTC-дней, отсутствует в последние 7. NULL до 14 дней истории / нулевой базы. Это не окончательный уход.",
        sql("product-p9-churn.sql"), "product", box(0, 168, 192, 28, 10), kind="table"),
]

# Current accounts are a database gauge, separate from historical pseudonymous profiles.
for panel in product_panels:
    panel["layout"]["y"] += 8
    panel["layout"]["i"] += 2
product_panels.insert(0, promql_panel(
    "registered", "Аккаунтов сейчас в базе",
    "Прямой агрегат MongoDB, один профиль на Telegram-аккаунт. Счётчик собирается раз в минуту. Экран обновляется только при включённом автообновлении или нажатии «Обновить». Исторические ID ниже могут включать удалённые профили.",
    "max(reki_players_registered)", "Текущие профили", box(0, 0, 96, 8, 1), kind="metric"))

product_panels.insert(1, history_panel(
    "dau-today", "Активных профилей сегодня · UTC",
    "Уникальные ID профилей с подтверждёнными событиями игры за текущую UTC-дату (03:00–03:00 МСК). Повторные входы одного профиля считаются один раз. Удаление и пересоздание даёт новый ID. Открытие только чата с ботом не является входом в игру. Включите автообновление 1m; при «Выкл» значение на экране устаревает.",
    sql("product-dau-today.sql"), "product", box(96, 0, 96, 8, 2), kind="metric"))
for panel in product_panels:
    if panel["type"] == "metric":
        panel["config"].update({"decimals": 0})

for panel in product_panels:
    if panel["type"] in ("stacked", "bar"):
        panel["config"].update({"unit": "percent", "y_axis_min": 0, "y_axis_max": 100})

PRODUCT_LABELS = {
    "segment": "Сегмент", "dimension": "Разрез", "observed_profiles_90d": "ID профилей в истории",
    "telemetry_since_utc": "Начало истории", "mau_30d": "MAU · 30 дней", "wau_7d": "WAU · 7 дней",
    "dau_today": "DAU · сегодня UTC", "dau_yesterday": "DAU · вчера UTC", "stickiness_yesterday_pct": "DAU/MAU · вчера, %",
    "active_users": "Активные профили", "active_player_days": "Активные игроко-дни", "completed_posts": "Подтверждённые посты",
    "posts_per_user_7d": "Постов / профиль за 7 дней", "posts_per_active_day": "Постов / активный день",
    "measured_player_days": "Дней с замером времени", "avg_active_minutes": "Среднее минут / активный день",
    "median_active_minutes": "Медиана минут / активный день", "mau_30d_yesterday": "MAU · база вчера",
    "stickiness_pct": "DAU/MAU, %", "player_days_pct": "Доля активных дней, %", "users_pct": "Доля профилей, %",
    "observed_users": "ID профилей · база", "first_post_users": "Дошли до 1 поста", "first_post_pct": "1 пост, %",
    "third_post_users": "Дошли до 3 постов", "third_post_pct": "3 поста, %", "median_hours_to_3_posts": "Медиана часов до 3 постов",
    "level_up_users": "Повысили навык", "level_up_pct": "Повысили навык, %", "card_users": "Получили награду", "card_pct": "Награда, %",
    "cohort_day": "День первого наблюдения", "cohort_users": "Профилей в когорте", "eligible_d1": "Готовая база D1",
    "returned_d1": "Вернулись D1", "retention_d1_pct": "D1, %", "eligible_d7": "Готовая база D7", "retention_d7_pct": "D7, %",
    "eligible_users": "База прошлой недели", "inactive_users": "Не вернулись за неделю", "weekly_churn_pct": "Неактивны, %",
    "day": "День", "dau": "Активные профили",
}
for panel in product_panels:
    for query in panel["queries"]:
        for axis_fields in ("x", "y", "breakdown"):
            for field in query["fields"].get(axis_fields, []):
                field["label"] = PRODUCT_LABELS.get(field["column"], field["label"])

product = {"org_id": "default", "request_body": {"title": "Реки: продукт", "description": "Аудитория, stickiness, время игры, посты на игрока, распределения активности и темп прогресса; возврат D1/D7 и недельная неактивность. Публикации подтверждаются сервером. Время и новые возраста требуют релиза приложения.", "tabs": [{"tabId": "product", "name": "Продукт", "panels": product_panels}]}}

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

def main():
    for name, body in (("product", product), ("load-and-llm", tech)):
        path = os.path.join(HERE, name + ".json")
        with open(path, "w", encoding="utf-8") as fh:
            json.dump(body, fh, ensure_ascii=False, indent=2)
            fh.write("\n")
        print(path, "| панелей:", len(body["request_body"]["tabs"][0]["panels"]))


if __name__ == "__main__":
    main()

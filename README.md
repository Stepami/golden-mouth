# Златоуст: игра «Реки»

Telegram Mini App 14+: ведёшь канал блогера в вымышленном мире, AI-звёзды и фанаты реагируют на каждый пост.

| Где | Что |
|---|---|
| [docs/game-flow.md](docs/game-flow.md) | Флоу игры и правила: источник правды |
| [docs/backend-spec.md](docs/backend-spec.md) | Спека бэкенда: ход, оценка поста LLM, контент миров, бот, безопасность, антифрод, аналитика |
| [docs/config/balance.json](docs/config/balance.json) | Все числа баланса одним конфигом |
| [docs/analytics.md](docs/analytics.md) | Продуктовая аналитика: вопросы, события, воронки, метрики, хранение |
| [docs/dashboards.md](docs/dashboards.md) | Шесть дашбордов Metabase: графики, как считать, какие решения принимать |
| [docs/screens/](docs/screens/README.md) | Макеты всех экранов |
| [docs/audit.md](docs/audit.md) | Итоговый аудит игры и экранов, вердикт для вёрстки |
| [docs/design/](docs/design/README.md) | Исходники экранов (HTML → PNG) |
| [docs/balance-sim/](docs/balance-sim/README.md) | Симуляции баланса и их выводы |
| [.agents/skills/](.agents/skills) | Навыки агента: `game-balance-sim` (симуляция баланса), `repo-consistency-check` (сверка на противоречия), `screen-check` (экраны против макетов) |
| [frontend/](frontend/README.md) | Mini App: React + TypeScript + Vite, Docker |
| [Issues](https://github.com/Stepami/golden-mouth/issues) | Задачи со спецификацией в теле |

Репозиторий публичный: токены, ключи, адреса серверов и реальные данные людей сюда не кладём.

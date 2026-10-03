---
name: issue-spec
description: Завести или обновить issue в golden-mouth в формате hydrascript со спецификацией в теле. Использовать на «заведи задачу», «оформи issue», «обнови спеку в задаче».
---

# Issue со спецификацией

**Заголовок:** `[feature request] …`, `[bug] …` или `[refactoring] …`.
**Метки:** тип (`Feature`, `bug`, `Improvement`) + область (`FrontEnd`, `BackEnd`, `AI`, `Safety`, `CI-CD`, `Spec`). **Milestone:** `v0.1 MVP`. **Исполнитель:** бэкенд, AI, CI → `Stepami`.

**Тело:**

```
# Specification: <коротко>

Status: Proposed; implementation is not part of this document.
Date: YYYY-MM-DD.

## Objective
Зачем это игроку или проекту. Ссылка на раздел docs/game-flow.md (полный URL на github.com, относительные ссылки в issue не работают).

## Implementation
Нумерованные шаги: экраны (коды как в docs/screens), API, данные.

## Acceptance criteria
- Проверяемые условия «готово».

## Additional context
Макеты (полные ссылки на PNG), связанные issues.
```

**Правила:**
- Числа не переписывать из головы: брать из `docs/config/balance.json` или ссылаться на него.
- Обновляешь задачу: правь нужный раздел (Python + `gh issue edit --body-file`), не дописывай в конец тела.
- Перед созданием проверить открытые issues: нет ли дубля (`gh issue list`). Дубль закрыть с ссылкой на основной.
- Публичный репозиторий: в тексте никаких секретов, адресов серверов и реальных данных.

# Исходники экранов

Экраны собираются из HTML в PNG 1170×2532 через headless Chrome: `python3 docs/design/src/build_journey.py [коды экранов]`, результат в [docs/screens/](../docs/screens/README.md).

- `src/build.py`: стили и общие блоки (берётся часть до `S={}`).
- `src/build_journey.py`: базовые экраны пути игрока.
- `src/journey_v3.py` … `journey_v7.py`: правки поверх базы по порядку (UX-аудит, встреча 02.10, комментарии, аудит геймдизайна, итоговая сверка). Новая правка = новый `journey_vN.py`, подключить в конце `build_journey.py`.
- `avatars/`: аватарки персонажей, лицензии в [avatars/README.md](avatars/README.md).

Персонажи вымышленные. Системные тексты без рода, AI-тексты в форме, выбранной игроком.

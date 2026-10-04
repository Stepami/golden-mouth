# Развёртывание в production

Production разворачивается на self-hosted GitHub Actions runner, установленном на VDS. Тег семантической версии, например `0.1.0`, запускает проверки бэкенда и фронтенда на GitHub-hosted runners, создаёт черновик GitHub Release, собирает Docker images на VDS, разворачивает их через Docker Compose и публикует релиз.

## Одноразовая настройка сервера

1. Направь `A`-запись домена `reki-app.ru` на VDS. Добавляй `AAAA`-запись только при настроенном IPv6 на сервере.
2. В firewall Timeweb Cloud и firewall операционной системы разреши входящие TCP-порты `22`, `80` и `443`. Не открывай наружу `27017`, `3000`, `8081` и `18888`.
3. Установи Docker Engine с Compose plugin. Выдай сервисному пользователю runner право запускать Docker, затем перезапусти службу runner.
4. Убедись, что версия GitHub Actions runner не ниже `2.329.0`. На self-hosted runner выполняются только checkout, сборка Docker images и deployment; .NET SDK и Node.js на VDS для CI не требуются.
5. Размести цепочку сертификатов и закрытый ключ вне репозитория, например:

   ```text
   /etc/reki/tls/fullchain.pem
   /etc/reki/tls/private.key
   ```

   `fullchain.pem` должен содержать сертификат домена, а следом промежуточные сертификаты центра сертификации из Timeweb. Ограничь чтение закрытого ключа пользователем `root` и группой, от имени которой выполняется deployment.
6. Создай на VDS файл `/etc/reki/deploy.env`:

   ```dotenv
   REKI_DOMAIN=reki-app.ru
   REKI_TLS_CERT_PATH=/etc/reki/tls/fullchain.pem
   REKI_TLS_KEY_PATH=/etc/reki/tls/private.key
   ```

   Этот файл и TLS-материалы нельзя добавлять в Git. Пользователь runner должен читать `deploy.env`, а Docker — монтировать оба TLS-файла.
7. В настройках репозитория создай GitHub Environment с именем `production`. Ограничь право создания release-тегов и защити основную ветку.

Проверки бэкенда и фронтенда выполняются на `ubuntu-latest`. Setup actions сначала используют runtime из tool cache GitHub-hosted runner и скачивают его только при отсутствии подходящей версии. Self-hosted runner не расходуется на CI и участвует только в deployment job.

## Подготовка релиза

GitReleaseManager формирует описание релиза из GitHub issues, включённых в milestone. Перед созданием тега:

1. Создай milestone с именем будущей версии, например `0.1.0`.
2. Добавь в него завершённые issues этого релиза.
3. Назначь каждому issue один из release-labels:

   - `breaking change`;
   - `bug`;
   - `dependencies`;
   - `documentation`;
   - `feature`;
   - `improvement`.

Label `build` исключён из release notes. Настройки группировки находятся в `GitReleaseManager.yml`. GitReleaseManager добавляет в описание ссылки на issues и список участников; секция SHA и release-артефакты отключены.

Создай и отправь тег без префикса `v`:

```bash
git tag 0.1.0
git push origin 0.1.0
```

Release workflow выполняет следующие стадии:

1. Проверяет формат тега на GitHub-hosted runner.
2. Параллельно запускает полные backend и frontend проверки на GitHub-hosted runners.
3. Создаёт draft release через GitReleaseManager. Отсутствующий milestone или issues с подходящими labels останавливают pipeline до deployment.
4. На self-hosted runner собирает images `reki-backend:0.1.0` и `reki-frontend:0.1.0`, запускает Compose и ждёт healthy-состояния всех сервисов.
5. После успешного deployment публикует GitHub Release. Файлы и Docker images к нему не прикладываются.

Публичная точка входа — `https://reki-app.ru`; HTTP перенаправляется на HTTPS. Production runtime config переключает фронтенд с mock-данных на backend по same-origin адресу `/api/v1`.

При продлении купленного сертификата атомарно замени оба TLS-файла и перезапусти контейнер `gateway` из checkout репозитория. При ручном вызове Compose задай `REKI_VERSION`, равный текущему deployment-тегу.

## Закрытый доступ через SSH

MongoDB, Metabase, Aspire Dashboard, прямой порт фронтенда и прямой порт бэкенда слушают только loopback-интерфейс VDS. Открой один туннель с доверенного компьютера:

```bash
ssh -N \
  -L 27017:127.0.0.1:27017 \
  -L 3000:127.0.0.1:3000 \
  -L 18888:127.0.0.1:18888 \
  <user>@<server>
```

Пока туннель открыт:

- подключай MongoDB Compass к `mongodb://127.0.0.1:27017/reki`;
- открывай Metabase по адресу `http://127.0.0.1:3000`;
- открывай Aspire Dashboard по адресу `http://127.0.0.1:18888`.

Aspire работает без собственной авторизации, но его порт доступен только после аутентификации по SSH. Metabase дополнительно использует собственную учётную запись. Используй SSH-ключи, отключи вход по паролю после проверки доступа по ключу и по возможности ограничь порт `22` доверенными исходными IP-адресами.

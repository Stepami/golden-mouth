# Развёртывание в production

## Ключи и настройки входа (#123)

Модели Infinity, включая ONNX, публичные и скачиваются без HF_TOKEN. Пароль служебного пользователя `mongot` задан в Compose фиксированным техническим значением и не считается секретом; отдельный `MONGO_SEARCH_PASSWORD` в GitHub Actions не нужен. На первом запуске Infinity скачивает модели, поэтому ожидание готовности составляет до 15 минут.

Один раз сгенерируй RSA PEM для `AUTH_SIGNING_KEY` и 32 случайных байта в Base64 для `AUTH_ENCRYPTION_KEY`. Храни оба значения только в GitHub Actions Secrets и передавай через Compose environment; при каждом деплое используй те же значения, чтобы действующие refresh-токены оставались читаемыми. Пример одноразовой генерации (вывод не коммить и не публикуй):

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:3072
openssl rand -base64 32
```

Публичный production issuer задан в `compose.production.yml`, а URL двух Cloudflare Workers — в общем `compose.yml`; отдельные GitHub Actions Variables для них не нужны. OAuth Worker отдаёт metadata/JWKS со своим `jwks_uri`, Bot API Worker принимает запросы Telegram.Bot по корневому URL. Backend берёт публичный Telegram Client ID из `BotId` SDK, извлечённого из `TG_PAT`; формат токена не является публичным контрактом Telegram. В BotFather добавь production origin в Allowed URLs для Login JS. Для id_token flow Telegram Client Secret не требуется. `TG_PAT` остаётся секретом Mini App и Bot API.

Для локального backend задай `Auth__Issuer`, `Telegram__OAuthUrl` и `Telegram__ApiUrl` через user secrets или environment; локальный Compose читает `AUTH_ISSUER`, а URL Worker берёт из общего `compose.yml`. HTTP для token endpoint разрешён только в Development. В production Compose включает стандартную обработку `X-Forwarded-Proto` через `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`; внешний HTTPS передаётся gateway и frontend. Backend не публикуется напрямую в интернет.

Production разворачивается на self-hosted GitHub Actions runner, установленном на VDS. Push в `master` запускает проверки бэкенда, фронтенда и секретов. Затем GitVersion вычисляет версию, workflow создаёт тег и выкатывает соответствующий коммит. GitHub Release для уже развёрнутого тега создаётся отдельно ручным workflow `GitHub Release`.

## Одноразовая настройка сервера

Инструкция ниже рассчитана на Ubuntu 24.04 без установленной панели администрирования. Если на VDS другая ОС, команды установки Docker будут отличаться. В примерах пользователь runner называется `github-runner`; если runner уже установлен под другим пользователем, подставляй его имя.

### 1. Найти IP-адрес VDS и проверить SSH

1. Открой [панель Timeweb Cloud](https://timeweb.cloud/my/).
2. Перейди в раздел **Облачные серверы**.
3. Найди нужный VDS и скопируй его публичный IPv4 — достаточно кликнуть по адресу в карточке сервера.
4. Кликни по серверу. На вкладке **Дашборд** находятся пользователь, пароль и готовая команда подключения по SSH.
5. На своём компьютере открой PowerShell или терминал и подключись:

   ```bash
   ssh root@<PUBLIC_IPV4>
   ```

   Вместо `<PUBLIC_IPV4>` подставь скопированный адрес. При первом подключении SSH спросит, доверять ли fingerprint сервера: сверь его с данными панели, затем ответь `yes`.
6. На VDS проверь ОС и архитектуру:

   ```bash
   cat /etc/os-release
   uname -m
   ```

   Текущая production-площадка рассчитана на Linux x64: архитектура должна отображаться как `x86_64`. Сам deployment job выбирает runner по custom label `reki-prod`. Если SSH не подключается, открой **Облачные серверы → нужный сервер → Консоль** и проверь сервер из веб-консоли.

Официальная справка: [управление облачными серверами Timeweb Cloud](https://timeweb.cloud/docs/cloud-servers/manage-servers).

### 2. Направить домен на VDS в панели Timeweb Cloud

1. В панели открой **Домены и SSL**.
2. Кликни по домену `reki-app.ru`.
3. Перейди на вкладку **DNS**.
4. Найди запись типа `A` для основного домена:

   - если запись уже есть, кликни по ней и замени значение на публичный IPv4 VDS;
   - если записи нет, нажми **Добавить запись**, выбери тип `A`, основной домен в поле хоста и укажи IPv4 VDS. Можно выбрать сервер из списка сервисов аккаунта либо ввести IP вручную;
   - TTL оставь стандартным — `600` секунд.

5. Удали другие `A`-записи основного домена, ведущие на старые серверы. Записи почты (`MX`, `TXT`, `DKIM`) не трогай.
6. Запись `AAAA` добавляй только если VDS действительно получил публичный IPv6 и он настроен в ОС. Если в DNS уже есть `AAAA`, но IPv6 на VDS не работает, удали её: иначе часть браузеров будет пытаться открыть сайт по нерабочему адресу.
7. Сохрани изменения. Обновление `A`-записи обычно занимает от 15 минут до 3 часов.

Проверить DNS с Windows можно так:

```powershell
Resolve-DnsName reki-app.ru -Type A
```

С Linux или macOS:

```bash
dig +short A reki-app.ru
```

Команда должна вернуть публичный IPv4 VDS. До этого момента не проверяй сертификат через браузер. Подробности: [управление DNS-записями Timeweb Cloud](https://timeweb.cloud/docs/domains/dns-records-management).

### 3. Настроить облачный Firewall в панели Timeweb Cloud

Облачный Firewall фильтрует трафик до того, как он попадёт на VDS. Настрой его до запуска контейнеров.

1. Открой **Облачные серверы → нужный сервер → Сеть**.
2. В блоке **Firewall** нажми **Настроить → Создать новый**. Альтернативный путь: **Сети → Firewall → Добавить**.
3. Выбери режим **Разрешить трафик**. Это whitelist: всё, чего нет в разрешающих правилах, будет заблокировано.
4. В блоке **Входящий трафик** добавь правила:

   | Источник | Протокол | Порт | Для чего |
   |---|---|---:|---|
   | твой публичный IP с маской `/32` | TCP | `22` | SSH |
   | `0.0.0.0/0` | TCP | `80` | HTTP и перенаправление на HTTPS |
   | `0.0.0.0/0` | TCP | `443` | HTTPS |

   Свой текущий IP записывай в виде `203.0.113.10/32`. Если адрес часто меняется и ты не знаешь его, временно разреши порт `22` для `0.0.0.0/0`, проверь подключение, а затем сузь правило.
5. Не создавай исходящие правила: если в разрешающем firewall добавить хотя бы одно исходящее правило, весь остальной исходящий трафик будет заблокирован. Runner и Docker должны обращаться к GitHub и registry по HTTPS.
6. В блоке подключения сервисов выбери свой VDS, назови группу, например `reki-production`, и нажми **Создать**.
7. Не закрывай текущую SSH-сессию. Открой вторую и убедись, что SSH всё ещё работает.

Порты `27017`, `5080`, `8080` и `8081` в Timeweb открывать не нужно. MongoDB, OpenObserve и прямые порты фронтенда и backend доступны только через loopback VDS и SSH-туннель.

Если используешь IPv6, отдельно добавь правила `::/0` для портов `80` и `443`. Официальная справка: [управление Firewall Timeweb Cloud](https://timeweb.cloud/docs/firewall/upravlenie-fajrvolom).

### 4. Настроить firewall внутри Ubuntu

Это второй слой защиты. Сначала обязательно проверь облачный Firewall и сохрани открытую SSH-сессию.

```bash
sudo apt-get update
sudo apt-get install --yes ufw
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
sudo ufw status verbose
```

После `ufw enable` снова открой вторую SSH-сессию. Если она не подключается, не закрывай первую: исправь правило из неё. Основной периметр для опубликованных Docker-портов всё равно задаёт облачный Firewall Timeweb, поэтому не отключай его после настройки UFW.

### 5. Установить Docker Engine и Compose plugin

Сначала проверь, не установлен ли Docker уже:

```bash
docker version
docker compose version
```

Для healthcheck с частыми проверками при запуске нужны Docker Engine 25.0+ и Compose plugin 2.20.2+. Если обе команды отработали и версии подходят, переходи к выдаче доступа runner. Если Docker отсутствует или версия слишком старая, добавь официальный apt-репозиторий Docker:

```bash
sudo apt-get update
sudo apt-get install --yes ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

sudo tee /etc/apt/sources.list.d/docker.sources > /dev/null <<EOF
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}")
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF

sudo apt-get update
sudo apt-get install --yes docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo systemctl enable --now docker
sudo docker run --rm hello-world
```

Определи, от какого пользователя работает уже установленный runner:

```bash
ps -eo user,cmd | grep '[R]unner.Listener'
```

Добавь этого пользователя в группу `docker`. Ниже предполагается имя `github-runner`:

```bash
sudo usermod -aG docker github-runner
sudo -u github-runner -H docker version
sudo -u github-runner -H docker compose version
```

Если проверка вернула `permission denied`, перезапусти службу runner из каталога его установки:

```bash
sudo ./svc.sh stop
sudo ./svc.sh start
sudo ./svc.sh status
```

Членство в группе `docker` фактически даёт root-доступ к серверу. Не используй этого пользователя для посторонних процессов. Команды установки сверены с [официальной инструкцией Docker для Ubuntu](https://docs.docker.com/engine/install/ubuntu/) и [инструкцией по доступу без `sudo`](https://docs.docker.com/engine/install/linux-postinstall/).

### 6. Проверить или зарегистрировать self-hosted runner

Deployment job использует `runs-on: reki-prod`. Добавь custom label `reki-prod` только новому production runner. Это label, а не имя runner: совпадение имени не выбирает машину для job. Если runner уже зарегистрирован, повторная регистрация не нужна; label можно добавить через **Settings → Actions → Runners → нужный runner → Labels**. Во время переноса оставляй старый runner остановленным, а новый запускай только после восстановления томов, сертификатов и подготовки deployment-тега.

1. Открой репозиторий на GitHub.
2. Перейди **Settings → Actions → Runners**.
3. Проверь, что production runner установлен на Linux x64 и имеет custom label `reki-prod`. Стандартные labels `self-hosted`, `linux`, `x64` сохраняются; deployment job выбирает машину по `reki-prod`.
4. Если runner отсутствует, нажми **New self-hosted runner**, выбери **Linux** и **x64**.
5. GitHub покажет актуальные команды скачивания и регистрации с одноразовым токеном. Выполни их на VDS именно в том порядке, который показан на странице. Токен регистрации живёт один час; его нельзя сохранять в репозитории.
6. Выполняй runner не от `root`, а от отдельного пользователя. Например:

   ```bash
   sudo adduser --disabled-password --gecos "" github-runner
   sudo install -d -o github-runner -g github-runner /opt/actions-runner
   sudo -iu github-runner
   cd /opt/actions-runner
   ```

7. После выполнения показанных GitHub команд и `./config.sh` выйди из shell пользователя и установи runner как службу:

   ```bash
   exit
   cd /opt/actions-runner
   sudo ./svc.sh install github-runner
   sudo ./svc.sh start
   sudo ./svc.sh status
   ```

8. Вернись в **Settings → Actions → Runners**. Статус должен стать **Idle**. Не используй флаг `--disableupdate`: обычный self-hosted runner умеет обновляться автоматически.

### 7. Создать учётные данные OpenObserve

В GitHub открой **Settings → Secrets and variables → Actions** и добавь два repository secrets:

- `OPENOBSERVE_EMAIL` — выбранный командой email/login администратора OpenObserve;
- `OPENOBSERVE_PASSWORD` — случайный длинный пароль, например результат `openssl rand -base64 32`.

Это не выданные провайдером значения: до первого deployment команда сама выбирает email и пароль. На первом старте контейнер создаст root-пользователя с этой парой. Collector использует её только внутри Compose-сети для OTLP ingestion. Других токенов OpenObserve для MVP создавать не нужно. Не записывай значения в `.env`, compose-файлы, issue или документацию.

После инициализации `openobserve-data` одной замены GitHub Secrets недостаточно: она не меняет пароль уже созданного root-пользователя. Для ротации сначала измени пароль в OpenObserve или выполни официальный `openobserve reset -c root`, затем обнови оба GitHub Secrets, чтобы Collector продолжил проходить аутентификацию.

Официальная справка: [добавление self-hosted runner](https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/add-runners) и [запуск runner как systemd-службы](https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/configure-the-application?platform=linux).

### 8. Скопировать купленный SSL-сертификат из Timeweb Cloud

Балансировщик Timeweb и отдельный nginx в Ubuntu не нужны: порты `80` и `443`, TLS и перенаправление обслуживает контейнер `gateway` из `compose.production.yml`.

1. В панели Timeweb открой **Домены и SSL → SSL-сертификаты**.
2. Выбери сертификат для `reki-app.ru` и убедись, что он выпущен, а не находится в статусе обработки.
3. На странице сертификата скопируй **CRT** и **Private Key**. Timeweb выдаёт CRT с необходимой цепочкой сертификатов; сохрани весь блок целиком.
4. На VDS создай каталог. Если runner работает не от `github-runner`, замени имя пользователя:

   ```bash
   RUNNER_USER=github-runner
   RUNNER_GROUP="$(id -gn "$RUNNER_USER")"
   sudo install -d -m 750 -o root -g "$RUNNER_GROUP" /etc/reki/tls
   ```

5. Открой файл сертификата, вставь CRT целиком, сохрани и выйди:

   ```bash
   sudo nano /etc/reki/tls/fullchain.pem
   ```

   Файл должен начинаться строкой `-----BEGIN CERTIFICATE-----`.
6. Аналогично вставь Private Key:

   ```bash
   sudo nano /etc/reki/tls/private.key
   ```

   Файл должен начинаться со служебной PEM-строки `BEGIN … PRIVATE KEY`.
7. Ограничь доступ и проверь формат:

   ```bash
   sudo chown root:"$RUNNER_GROUP" /etc/reki/tls/fullchain.pem /etc/reki/tls/private.key
   sudo chmod 640 /etc/reki/tls/fullchain.pem /etc/reki/tls/private.key
   sudo openssl x509 -in /etc/reki/tls/fullchain.pem -noout -subject -issuer -dates
   sudo openssl pkey -in /etc/reki/tls/private.key -noout -check
   ```

Не отправляй Private Key в чат, issue, GitHub Secrets или репозиторий. Справка Timeweb: [заказ, получение CRT и Private Key](https://timeweb.cloud/docs/ssl/ssl-order-and-renewal) и [установка SSL](https://timeweb.cloud/docs/ssl/installing-ssl).

Проверки бэкенда и фронтенда выполняются на `ubuntu-latest`; self-hosted runner участвует только в deployment job и не требует установленных .NET SDK или Node.js. Создавать GitHub Environment для текущей схемы не нужно.

### 9. Проверить готовность перед первой выкладкой

1. Убедись, что [frontend runtime config](../frontend/public/runtime-config/config.js) содержит `apiMode: 'backend'` и изменение закоммичено до push в `master`.
2. Проверь, что порты `80` и `443` не заняты системным nginx, Apache или другим приложением:

   ```bash
   sudo ss -ltnp | grep -E ':(80|443)[[:space:]]'
   ```

   До первого deployment команда не должна ничего вывести. Если вывод есть, выясни, какой сервис занял порт, и отключи его до запуска Compose.
3. В GitHub открой **Settings → Actions → Runners** и убедись, что runner имеет статус **Idle**.
4. Ещё раз проверь DNS: `reki-app.ru` должен возвращать публичный IPv4 VDS.
5. После первого release workflow проверь на VDS:

   ```bash
   docker ps
   curl --fail http://127.0.0.1:8081/health/ready
   curl --head http://reki-app.ru
   curl --head https://reki-app.ru
   ```

   Первый запрос к домену должен вернуть перенаправление на HTTPS, второй — успешный HTTPS-ответ. Если deployment job остаётся в состоянии **Queued**, проверь, что новый runner включён, доступен репозиторию и имеет label `reki-prod`.

## Подготовка релиза

При переносе на RU-сервер Multitool вызывается напрямую через `LLM_BASE_URL`; production Compose не подменяет hostname провайдера через `extra_hosts`. Переменная `LLM_PROXY_IP` для нового deployment не нужна. Старый тег может содержать прежние Compose/workflow: первый deployment на новой площадке должен использовать тег с обновлённой конфигурацией.

OpenObserve закреплён на версии `v1.1.2` из зеркала Timeweb. По умолчанию `compose.yml` использует обычный образ `dockerhub.timeweb.cloud/openobserve/openobserve:v1.1.2`, совместимый с локальной разработкой без AVX-512. В `compose.production.yml` образ переопределён на `dockerhub.timeweb.cloud/openobserve/openobserve:v1.1.2-simd`. На x86-64 SIMD-образ требует AVX-512: перед переносом production на другую машину проверить CPU и доступность образа. Это согласованное обновление с `v1.0.4` на `v1.1.2` вместе со сменой registry на зеркало Timeweb. Перед первым запуском новой версии сохранить исходный backup данных; возврат старого image сам по себе не откатывает изменения данных новой версии.

После успешного запуска Compose и установки renewal hook deployment выполняет `docker system prune -af`: удаляет остановленные контейнеры, неиспользуемые сети, все неиспользуемые images и build cache. Флаг `--volumes` не используется, Docker volumes сохраняются. Очистка относится ко всему Docker daemon runner; старые images для отката после неё придётся загрузить или собрать заново. Если deployment или установка hook завершились ошибкой, шаг очистки пропускается.

GitReleaseManager формирует описание релиза из GitHub issues, включённых в milestone. Перед ручным запуском workflow `GitHub Release`:

1. Создай milestone с именем будущей версии, например `0.1.0`.
2. Добавь в него завершённые issues этого релиза.
3. Назначь каждому issue один из release-labels:

   - `bug`;
   - `BackEnd`;
   - `FrontEnd`;
   - `CI-CD`.

Issues без одного из этих четырёх labels не попадают в release notes. Настройки группировки находятся в `GitReleaseManager.yml`. GitReleaseManager добавляет в описание ссылки на issues и список участников; секция SHA и release-артефакты отключены.

Тег без префикса `v` (например, `0.1.0`) создаётся автоматически. Конфигурация GitVersion находится в `GitVersion.yml`; вычисленная версия используется только как тег и значение `REKI_VERSION` для Docker Compose.

Автоматический workflow `Release` выполняет следующие стадии:

1. Параллельно запускает полные backend и frontend проверки и сканирование секретов на GitHub-hosted runners.
2. Вычисляет версию через GitVersion и отправляет тег текущего коммита.
3. Передаёт версию в `deploy.yml`. Тот проверяет формат и существование тега, на self-hosted runner собирает images `reki-backend:0.1.0` и `reki-frontend:0.1.0`, запускает Compose и ждёт healthy-состояния всех сервисов.

После успешного deployment открой **Actions → GitHub Release → Run workflow**, введи существующий тег версии и запусти workflow. Он проверит формат версии, создаст draft release через GitReleaseManager, опубликует его и закроет одноимённый milestone. Отсутствующий milestone или issues с подходящими labels останавливают публикацию, но не влияют на уже выполненный deployment.

Healthcheck сервисов выполняется каждые 10 секунд при запуске контейнера и каждые 5 минут после перехода в healthy. Это сохраняет быстрое ожидание готовности при деплое, но уменьшает регулярную телеметрию от проверок.

Для атомарного сохранения публикации MongoDB работает как single-node replica set `rs0`. `mongo-init` однократно инициирует его на существующем томе и ждёт primary до запуска backend. Перед первым развёртыванием этой версии сделай резервную копию тома MongoDB и останови запись на время переключения; один узел даёт транзакции, но не отказоустойчивость. Если задана внешняя `MONGO_CONNECTION_STRING`, она должна указывать на MongoDB с поддержкой транзакций.

Для повторной выкладки открой **Actions → Deploy → Run workflow**, введи существующий тег версии и запусти workflow. Ручной запуск проверяет тег, собирает и разворачивает Docker images из соответствующего коммита. Он не запускает backend/frontend проверки и сканирование секретов и не создаёт новый тег. Создание GitHub Release и закрытие milestone выполняет отдельный ручной workflow `GitHub Release`.

Публичная точка входа — `https://reki-app.ru`; HTTP перенаправляется на HTTPS. Production использует тот же `frontend/public/runtime-config/config.js`, что и локальный Compose. Перед релизом установи в нём `apiMode: 'backend'`, чтобы фронтенд обращался к backend по same-origin адресу `/api/v1`.

При продлении купленного сертификата атомарно замени оба TLS-файла и перезапусти контейнер `gateway` из checkout репозитория. При ручном вызове Compose задай `REKI_VERSION`, равный текущему deployment-тегу.

## OpenObserve на отдельном поддомене

OpenObserve доступен через `https://openobserve.reki-app.ru`. Прямой порт `5080` остаётся привязан к loopback VDS; шлюз обращается к сервису по внутренней сети Compose. Сертификат поддомена выпускает Certbot на VDS через HTTP-01, основной сайт продолжает использовать свой сертификат из `/etc/reki/tls`.

Первый выпуск требует двух последовательных релизов: сначала конфигурацию с обработкой `/.well-known/acme-challenge/`, затем, после получения сертификата, конфигурацию с HTTPS virtual host и проверкой сертификата в `deploy.yml`. Если выкатить второй релиз раньше, `gateway` не сможет запуститься из-за отсутствующих файлов сертификата.

1. В Timeweb Cloud открой **Домены и SSL → `reki-app.ru` → DNS** и создай A-запись для `openobserve` с публичным IPv4 VDS. Не добавляй AAAA-запись без работающего IPv6. Проверь, что `openobserve.reki-app.ru` уже разрешается в этот IPv4. Порты `80` и `443` должны оставаться доступными извне.
2. На VDS создай каталог для проверки владения доменом:

   ```bash
   sudo install -d -m 755 /var/lib/reki/acme-webroot
   ```

3. Выкати первый релиз: `gateway` монтирует этот каталог в `/var/www/letsencrypt` и отдаёт файлы из `/.well-known/acme-challenge/` по HTTP. До выпуска сертификата не выкатывай HTTPS virtual host поддомена.
4. Установи Certbot на VDS и выпусти сертификат. Поскольку Nginx работает в контейнере, используй режим `webroot`, а не плагин `--nginx`:

   ```bash
   sudo apt-get update
   sudo apt-get install --yes certbot
   sudo certbot certonly --webroot \
     -w /var/lib/reki/acme-webroot \
     -d openobserve.reki-app.ru
   sudo test -r /etc/letsencrypt/live/openobserve.reki-app.ru/fullchain.pem
   sudo test -r /etc/letsencrypt/live/openobserve.reki-app.ru/privkey.pem
   ```

   Certbot спросит контактный email и согласие с условиями Let’s Encrypt. Не сохраняй email, сертификат или приватный ключ в репозитории. Если проверка домена не прошла, сначала проверь DNS и доступность HTTP challenge, не останавливая основной сайт.
5. Выкати второй релиз: `gateway` получит read-only доступ к `/etc/letsencrypt`, а отдельный HTTPS virtual host направит запросы в `openobserve:5080`. `deploy.yml` проверит наличие обоих файлов сертификата до запуска Compose. Каталог `/etc/letsencrypt` монтируется целиком, потому что файлы в `live/` ссылаются на файлы в `archive/`.
6. Второй релиз автоматически установит `.github/scripts/reload-openobserve-gateway.sh` в `/etc/letsencrypt/renewal-hooks/deploy/`. После успешного продления Certbot проверит конфигурацию и перечитает её в работающем `gateway`. Проверь продление:

   ```bash
   sudo certbot renew --dry-run --run-deploy-hooks
   systemctl list-timers certbot.timer
   ```

   Пакет Certbot для Ubuntu устанавливает `certbot.timer` для автоматического продления; убедись, что таймер отображается в выводе команды выше. Hook использует Docker labels проекта `reki` и сервиса `gateway`, поэтому не зависит от имени контейнера или каталога checkout. При следующем релизе workflow обновит hook на VDS.
7. Проверь `http://openobserve.reki-app.ru` (перенаправление на HTTPS), `https://openobserve.reki-app.ru` (сертификат и вход в OpenObserve) и `https://reki-app.ru` (основной сайт). Не открывай `5080` в Firewall Timeweb.

Справка: [поддомены Timeweb](https://timeweb.cloud/docs/domains/subdomains), [HTTP-01 Let’s Encrypt](https://letsencrypt.org/docs/challenge-types/), [режим webroot и renewal hooks Certbot](https://eff-certbot.readthedocs.io/en/stable/using.html).

## Ежедневная выгрузка метрик

`GET https://openobserve.reki-app.ru/api/v1/metrics/daily?date_from=YYYY-MM-DD&date_to=YYYY-MM-DD` принимает включительный диапазон до 90 московских суток. Возьми значение из заголовка `Authorization: Basic …` сервисной учётной записи OpenObserve и передай его сборщику как `Authorization: Bearer …`. Значение после схемы — готовая строка Base64 от `идентификатор:токен`. Шлюз заменяет `Bearer` на `Basic`, а OpenObserve проверяет доступ к `/_search`.

Токены из раздела «Токены загрузки» предназначены для записи телеметрии. Для выгрузки нужен токен из раздела «Сервисные учётные записи». Он также действует при прямом обращении к API OpenObserve в пределах прав этой учётной записи.

DAU учитывает создание мира, публикацию и комментарий. Счётчик LLM использует chat-спаны публикаций, комментариев и связанных фоновых задач. Метрики включают тестовых игроков. `total_users` берётся из последнего замера Mongo за сутки. Замер обновляется раз в 30 минут. Если за одну из дат замера нет, API возвращает `503`. Перед подключением сверь историю с датой старта маркетинга и укажи эти отличия в форме.

## Закрытый доступ через SSH

MongoDB, прямой порт OpenObserve, прямой порт фронтенда и прямой порт бэкенда слушают только loopback-интерфейс VDS. OpenObserve также доступен через HTTPS-шлюз на `openobserve.reki-app.ru`. Если нужен прямой доступ к одному из loopback-портов, открой туннель с доверенного компьютера:

```bash
ssh -N \
  -L 27017:127.0.0.1:27017 \
  -L 5080:127.0.0.1:5080 \
  <user>@<server>
```

Пока туннель открыт:

- подключай MongoDB Compass через туннель с `directConnection=true` (адрес single-node replica set внутри Compose недоступен с локального компьютера);
- открывай OpenObserve по адресу `http://127.0.0.1:5080` и входи с `OPENOBSERVE_EMAIL` / `OPENOBSERVE_PASSWORD`.

Используй SSH-ключи, отключи вход по паролю после проверки доступа по ключу и по возможности ограничь порт `22` доверенными исходными IP-адресами.

## Фоновые задачи (#146)

Планировщик TickerQ работает в backend и хранит расписания и историю в текущей MongoDB. Восстановление энергии проверяется раз в две минуты на 11-й секунде, количество игроков обновляется раз в 30 минут на 15-й секунде, уведомления обрабатываются раз в пять минут на 40-й секунде. При старте backend задачи обслуживания не запускаются. Трейсы задач экспортируются через существующий Collector; дополнительные endpoints и exporter не нужны.

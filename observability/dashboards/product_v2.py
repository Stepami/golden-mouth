"""Decision-oriented product panels; SQL has common segment filters and MSK days."""
import json
from pathlib import Path

HERE = Path(__file__).parent
DAY = 86400000000
SHIFT = 10800000000

COMMON = """WITH clock AS (
 SELECT floor((max(date_part('epoch', now())) * 1000000 + 10800000000) / 86400000000) AS today,
 max(date_part('epoch', now())) * 1000 AS now_ms, count(*) AS stream_rows FROM product),
world_ranked AS (SELECT player_id, world_id, world, subscriber_total,
 row_number() OVER (PARTITION BY player_id, world_id ORDER BY _timestamp DESC) AS state_rank
 FROM product WHERE event_name = 'world_audience_updated' AND world_id IS NOT NULL),
world_audience AS (SELECT player_id, sum(subscriber_total) AS subscribers,
 CASE WHEN count(DISTINCT world) > 1 THEN 'multi-world' ELSE max(world) END AS world
 FROM world_ranked WHERE state_rank = 1 GROUP BY player_id),
deleted_players AS (SELECT DISTINCT player_id FROM product
 WHERE event_name = 'player_deleted' AND player_id IS NOT NULL),
raw AS (SELECT p.* FROM product p WHERE activity_kind = 'real' AND player_id IS NOT NULL AND player_id <> ''
 AND NOT EXISTS (SELECT 1 FROM deleted_players d WHERE d.player_id = p.player_id)),
identities AS (SELECT r.player_id, min(CASE WHEN event_name NOT IN ('active_time','notification_sent') THEN _timestamp END) AS first_seen,
 min(CASE WHEN event_name = 'player_created' THEN _timestamp END) AS registered_at,
 coalesce(max(w.world), CASE WHEN count(DISTINCT nullif(nullif(r.world, ''), 'unknown'))>1 THEN 'multi-world' ELSE max(nullif(nullif(r.world, ''), 'unknown')) END, 'unknown') AS world,
 coalesce(max(CASE WHEN age_group <> '18+' THEN nullif(age_group, '') END), max(nullif(age_group, '')), 'unknown') AS age_group,
 coalesce(max(nullif(registration_source, '')), 'unknown') AS registration_source,
 CASE WHEN count(DISTINCT CASE WHEN launch_surface <> 'unknown' THEN nullif(launch_surface, '') END) > 1 THEN 'multi'
 ELSE coalesce(max(CASE WHEN launch_surface <> 'unknown' THEN nullif(launch_surface, '') END), 'unknown') END AS launch_surface
 FROM raw r LEFT JOIN world_audience w ON r.player_id = w.player_id GROUP BY r.player_id),
selected AS (SELECT * FROM identities WHERE
 ('${world}' = 'all' OR world = '${world}') AND ('${age}' = 'all' OR age_group = '${age}')
 AND ('${source}' = 'all' OR registration_source = '${source}')
 AND ('${surface}' = 'all' OR launch_surface = '${surface}')),
visits AS (SELECT DISTINCT player_id, floor((_timestamp + 10800000000) / 86400000000) AS day
 FROM raw WHERE event_name NOT IN ('active_time', 'notification_sent')),
posts AS (SELECT player_id, coalesce(nullif(prop_id, ''), concat(player_id, ':legacy:', cast(event_time_unix_ms AS VARCHAR))) AS post_id,
 max(_timestamp) AS posted_at, max(coalesce(nullif(world, ''), 'unknown')) AS post_world,
 max(cast(prop_delta AS DOUBLE)) AS delta FROM raw WHERE event_name = 'post_scored' GROUP BY player_id, post_id),
selected_posts AS (SELECT p.* FROM posts p JOIN selected s ON p.player_id = s.player_id),
current_profiles AS (SELECT s.player_id, w.subscribers FROM selected s
 LEFT JOIN world_audience w ON s.player_id = w.player_id),
cohort AS (SELECT *, CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END AS cohort_at FROM selected CROSS JOIN clock WHERE CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END IS NOT NULL
 AND floor((CASE WHEN '${cohort_origin}'='observed' THEN first_seen ELSE registered_at END + 10800000000) / 86400000000) >= today - cast('${days}' AS INT) + 1)
"""


def variable(name, label, options):
 return {"name": name, "label": label, "type": "custom", "multiSelect": False,
         "value": options[0][1], "options": [{"label": a, "value": b, "selected": i == 0} for i, (a, b) in enumerate(options)],
         "hideOnDashboard": False}


def make(sql_panel, box, promql_panel):
 variables = {"list": [
 variable('days', 'Период анализа · дней', [('30', '30'), ('7', '7'), ('90', '90')]),
 variable('cohort_origin', 'База воронки и удержания', [('Подтверждённая регистрация','registered'),('Первое наблюдение · старые данные','observed')]),
 variable('world', 'Мир', [('Все', 'all'), ('Сцена', 'stage'), ('Академия', 'academy'), ('Айти', 'it'), ('Стрим', 'stream'), ('Свой', 'own'), ('Несколько миров · старая история', 'multi-world'), ('Неизвестно', 'unknown')]),
 variable('age', 'Возраст', [('Все', 'all'), ('14–17', '14-17'), ('18–24', '18-24'), ('25–34', '25-34'), ('35–44', '35-44'), ('45+', '45+'), ('18+ · старые данные', '18+'), ('Неизвестно', 'unknown')]),
 variable('source', 'Регистрация', [('Все', 'all'), ('По приглашению', 'referral'), ('Без приглашения', 'direct'), ('Неизвестно', 'unknown')]),
 variable('compare', 'Сравнить интенсивность по', [('Возрасту','age'),('Миру','world'),('Источнику','source'),('Способу запуска','surface')]),
 variable('surface', 'Способ запуска', [('Все', 'all'), ('Telegram Mini App', 'telegram-mini-app'), ('Браузер', 'browser'), ('Standalone/PWA', 'standalone'), ('Несколько способов', 'multi'), ('Неизвестно · старые данные', 'unknown')]),
 ]}
 panels=[]
 def add(id,title,desc,tail,y,h=12,kind='table',x=0,w=192,breakdown=None,values=None):
  query=COMMON+tail
  (HERE/'panel-sql'/f'v2-{id}.sql').write_text(query+'\n')
  p=sql_panel(id,title,desc,query,'product',box(x,y,w,h,len(panels)+1),kind=kind,breakdown=breakdown,values=values)
  p['config'].update({'panel_time_enabled':True,'panel_time_range':{'type':'relative','relativeTimePeriod':'90d'},'decimals':0 if kind=='metric' else 2})
  panels.append(p)
  return p
 panels.append(promql_panel('accounts', 'Всего аккаунтов в Mongo',
 'Точное количество профилей из CountDocumentsAsync; обновляется раз в минуту. Фильтры сегментов не применяются. Отсутствие метрики не означает ноль.',
 'max(reki_players_registered)', 'Аккаунты', box(0,0,96,8,len(panels)+1), kind='metric'))
 add('active-today','Активных аккаунтов сегодня · МСК','Наблюдаемые ID выбранного сегмента без доставленного player_deleted. С 00:00 МСК; это не онлайн.',
 "SELECT 'DAU' AS metric, count(DISTINCT p.player_id) AS dau FROM current_profiles p JOIN visits v ON p.player_id=v.player_id CROSS JOIN clock c WHERE v.day=c.today",0,8,'metric',x=96,w=96)
 add('current-audience','Наблюдаемая аудитория: DAU / WAU / MAU и подписчики','Доставленные события выбранного сегмента без удалённых ID. Общий точный счётчик Mongo расположен отдельно. Подписчики — последнее доставленное состояние миров; audience_known_accounts показывает покрытие. Старые удаления без события неизвестны.',
 """SELECT count(DISTINCT p.player_id) AS accounts,
 count(DISTINCT CASE WHEN v.day=c.today THEN p.player_id END) AS dau,
 count(DISTINCT CASE WHEN v.day>=c.today-6 THEN p.player_id END) AS wau,
 count(DISTINCT CASE WHEN v.day>=c.today-29 THEN p.player_id END) AS mau,
 (SELECT sum(subscribers) FROM current_profiles) AS channel_subscribers,
 (SELECT count(subscribers) FROM current_profiles) AS audience_known_accounts
 FROM clock c LEFT JOIN current_profiles p ON true LEFT JOIN visits v ON p.player_id=v.player_id""",8,12)
 add('dau','DAU по дням · МСК','Наблюдаемые ID выбранного сегмента, исключая доставленные удаления. Последний день неполный. Столбцы вместо сглаженной линии. Дни без посещений показаны нулём только после начала наблюдений.',
 """, all_days AS (SELECT floor((max(date_part('epoch',now()))*1000000+10800000000)/86400000000)-unnest(range(0,cast('${days}' AS BIGINT))) AS day FROM product), calendar AS (SELECT day FROM all_days WHERE day >= (SELECT floor((min(_timestamp)+10800000000)/86400000000) FROM raw)), selected_visits AS (SELECT v.* FROM visits v JOIN selected s ON v.player_id=s.player_id)
 SELECT to_timestamp_micros(d.day*86400000000-10800000000) AS day_at,count(DISTINCT v.player_id) AS active_profiles FROM calendar d LEFT JOIN selected_visits v ON d.day=v.day GROUP BY d.day ORDER BY d.day""",20,14,'bar')
 add('stickiness','Stickiness: средний DAU / MAU · полные дни','Средний DAU по календарным дням / уникальные активные ID за то же окно. Максимум 30 завершённых дней МСК; первый неполный день истории исключён. observed_days < 30 — предварительная оценка. История ID, не текущая база.',
 """, coverage AS (SELECT greatest(0,least(30,c.today-floor((min(r._timestamp)+10800000000)/86400000000)-1)) AS observed_days,c.today FROM raw r CROSS JOIN clock c GROUP BY c.today),
 stick AS (SELECT w.observed_days,count(DISTINCT v.player_id) AS mau,count(v.player_id) AS active_profile_days, sum(CASE WHEN v.player_id IS NOT NULL AND v.day>=w.today-7 THEN 1 ELSE 0 END) AS active_days_7d FROM coverage w LEFT JOIN (SELECT v.* FROM visits v JOIN selected s ON v.player_id=s.player_id) v ON v.day>=w.today-w.observed_days AND v.day<w.today GROUP BY w.observed_days)
 SELECT observed_days,mau,active_profile_days,round(1.0*active_profile_days/nullif(observed_days,0),2) AS mean_dau,round(1.0*active_days_7d/nullif(least(7,observed_days),0),2) AS mean_dau_7d,
 round(100.0*active_profile_days/nullif(observed_days*mau,0),1) AS stickiness_pct FROM stick""",34,12)
 add('funnel','Воронка: регистрация → онбординг → 1 / 3 / 10 постов','Регистрация и посты используют выбранную базу. Онбординг считается только среди новых регистраций с player_created.completed=false: users — завершившие, base — новые игроки. Старые регистрации не считаются отказами от онбординга. Переключатель первого наблюдения предназначен для старой истории; это прохождение, не retention.',
 """, onboarding AS (SELECT player_id,
 max(CASE WHEN event_name='player_created' AND prop_completed='false' THEN 1 ELSE 0 END) AS new_registration,
 max(CASE WHEN event_name='onboarding_completed' THEN 1 ELSE 0 END) AS completed
 FROM raw GROUP BY player_id),
 progression AS (SELECT c.player_id,count(p.post_id) AS posts,max(o.new_registration) AS new_registration,max(o.completed) AS completed
 FROM cohort c LEFT JOIN posts p ON c.player_id=p.player_id LEFT JOIN onboarding o ON c.player_id=o.player_id GROUP BY c.player_id), stages AS (
 SELECT 0 AS ord,CASE WHEN '${cohort_origin}'='observed' THEN 'Первое наблюдение' ELSE 'Регистрация' END AS stage,count(*) AS users,count(*) AS base FROM progression UNION ALL
 SELECT 1,'Онбординг · новые регистрации',count(CASE WHEN new_registration=1 AND completed=1 THEN 1 END),count(CASE WHEN new_registration=1 THEN 1 END) FROM progression UNION ALL
 SELECT 2,'Первый пост',count(CASE WHEN posts>=1 THEN 1 END),count(*) FROM progression UNION ALL
 SELECT 3,'Три поста',count(CASE WHEN posts>=3 THEN 1 END),count(*) FROM progression UNION ALL
 SELECT 4,'Десять постов',count(CASE WHEN posts>=10 THEN 1 END),count(*) FROM progression)
 SELECT stage,users,base,round(100.0*users/nullif(base,0),1) AS conversion_pct FROM stages ORDER BY ord""",46,14)
 add('retention','Возврат когорт: D1 / D3 / D7','Когорты по дням МСК. Переключатель базы: подтверждённая регистрация или первое наблюдение для старой истории. Точный день возврата. Возвращение в день регистрации не D1. Текущий день исключён из eligible. Разные знаменатели показаны явно; не объединять в воронку.',
 """, cohort_days AS (SELECT player_id,floor((cohort_at+10800000000)/86400000000) AS registered_day FROM cohort), returns AS (
 SELECT p.player_id,p.registered_day,c.today,max(CASE WHEN v.day=p.registered_day+1 THEN 1 ELSE 0 END) AS d1,
 max(CASE WHEN v.day=p.registered_day+3 THEN 1 ELSE 0 END) AS d3,max(CASE WHEN v.day=p.registered_day+7 THEN 1 ELSE 0 END) AS d7
 FROM cohort_days p CROSS JOIN clock c LEFT JOIN visits v ON p.player_id=v.player_id GROUP BY p.player_id,p.registered_day,c.today)
 SELECT to_timestamp_micros(registered_day*86400000000-10800000000) AS cohort_day,count(*) AS registered,
 sum(CASE WHEN registered_day+1<today THEN 1 ELSE 0 END) AS eligible_d1,
 sum(CASE WHEN registered_day+1<today THEN d1 ELSE 0 END) AS returned_d1,
 round(100.0*sum(CASE WHEN registered_day+1<today THEN d1 ELSE 0 END)/nullif(sum(CASE WHEN registered_day+1<today THEN 1 ELSE 0 END),0),1) AS d1_pct,
 sum(CASE WHEN registered_day+3<today THEN 1 ELSE 0 END) AS eligible_d3,
 sum(CASE WHEN registered_day+3<today THEN d3 ELSE 0 END) AS returned_d3,
 round(100.0*sum(CASE WHEN registered_day+3<today THEN d3 ELSE 0 END)/nullif(sum(CASE WHEN registered_day+3<today THEN 1 ELSE 0 END),0),1) AS d3_pct,
 sum(CASE WHEN registered_day+7<today THEN 1 ELSE 0 END) AS eligible_d7,
 sum(CASE WHEN registered_day+7<today THEN d7 ELSE 0 END) AS returned_d7,
 round(100.0*sum(CASE WHEN registered_day+7<today THEN d7 ELSE 0 END)/nullif(sum(CASE WHEN registered_day+7<today THEN 1 ELSE 0 END),0),1) AS d7_pct
 FROM returns GROUP BY registered_day ORDER BY registered_day""",60,26)
 add('posts-today','Завершённых постов сегодня · МСК','Серверный post_scored. Повторы одного стабильного post_id считаются один раз; старые события без post_id используют исходное event_time. Попытки и ошибки не являются результатом.',
 "SELECT 'Посты' AS metric,count(*) AS posts FROM selected_posts p CROSS JOIN clock c WHERE floor((p.posted_at+10800000000)/86400000000)=c.today",86,8,'metric',w=96)
 add('growth-today','Измеренный прирост сегодня · МСК','Сумма серверного delta после нового релиза, включая снижение. NULL — нет ни одного измеренного результата. Исторические события без delta не подменяются нулём; покрытие в таблице ниже.',
 "SELECT 'Прирост' AS metric,CASE WHEN count(*)=0 THEN 0 ELSE sum(p.delta) END AS subscriber_growth FROM selected_posts p CROSS JOIN clock c WHERE floor((p.posted_at+10800000000)/86400000000)=c.today",86,8,'metric',x=96,w=96)
 add('posts-daily','Посты пользователей по дням и мирам','Сохранённые завершённые публикации, выбранный сегмент. Мир — в момент публикации; фильтр «Мир» для профиля — последние доставленные состояния миров, затем известный мир истории.',
 "SELECT to_timestamp_micros(floor((posted_at+10800000000)/86400000000)*86400000000-10800000000) AS day,post_world AS world,count(*) AS posts FROM selected_posts CROSS JOIN clock WHERE floor((posted_at+10800000000)/86400000000)>=today-cast('${days}' AS INT)+1 GROUP BY day,post_world ORDER BY day",94,14,'stacked',breakdown='world',values=['posts'])
 add('growth-daily','Прирост игровых подписчиков по мирам','Чистый прирост по завершённым публикациям. Не охват и не реальная аудитория. Покрытие delta начинается с релиза; нет измерения — NULL.',
 "SELECT to_timestamp_micros(floor((posted_at+10800000000)/86400000000)*86400000000-10800000000) AS day,post_world AS world,sum(delta) AS subscriber_growth FROM selected_posts CROSS JOIN clock WHERE floor((posted_at+10800000000)/86400000000)>=today-cast('${days}' AS INT)+1 GROUP BY day,post_world ORDER BY day",108,14,'bar',breakdown='world',values=['subscriber_growth'])
 add('daily-results','Результаты игры: суммы, на профиль и покрытие','Дни МСК, сегодня неполный. active_profiles — исторические активные ID сегмента. Posts_with_delta показывает полноту измерения прироста. Средние по всем активным профилям, не только авторам.',
 """, days AS (SELECT v.day,count(DISTINCT v.player_id) AS active_profiles FROM visits v JOIN selected s ON v.player_id=s.player_id CROSS JOIN clock c WHERE v.day>=c.today-cast('${days}' AS INT)+1 GROUP BY v.day), results AS (
 SELECT floor((posted_at+10800000000)/86400000000) AS day,count(*) AS posts,count(delta) AS posts_with_delta,sum(delta) AS subscriber_growth FROM selected_posts GROUP BY day)
 SELECT to_timestamp_micros(d.day*86400000000-10800000000) AS day_at,d.active_profiles,coalesce(r.posts,0) AS posts,
 coalesce(r.posts_with_delta,0) AS posts_with_delta,r.subscriber_growth,
 round(1.0*coalesce(r.posts,0)/nullif(d.active_profiles,0),2) AS posts_per_active_profile,
 round(r.subscriber_growth/nullif(d.active_profiles,0),2) AS growth_per_active_profile FROM days d LEFT JOIN results r ON d.day=r.day ORDER BY d.day DESC""",122,24)
 add('launch','Способ запуска и устройства','Способ запуска измеряется явно с нового релиза. iOS/Android/desktop — устройство Telegram, не самостоятельное приложение. Старые события — unknown. Standalone без авторизации пока не поддерживает игру; отсутствие строк не доказывает нулевую аудиторию.',
 "SELECT coalesce(nullif(r.launch_surface,''),'unknown') AS launch_surface,coalesce(nullif(r.platform,''),'server') AS device,count(DISTINCT r.player_id) AS active_profiles FROM raw r JOIN selected s ON r.player_id=s.player_id CROSS JOIN clock c WHERE r.event_name IN ('session_start','screen_view','app_visible') AND floor((r._timestamp+10800000000)/86400000000)>=c.today-cast('${days}' AS INT)+1 GROUP BY coalesce(nullif(r.launch_surface,''),'unknown'),coalesce(nullif(r.platform,''),'server') ORDER BY active_profiles DESC",146,18)
 add('history-coverage','Покрытие данных и старые профили','Первое наблюдение ≠ регистрация. Для старых профилей источник неизвестен, если нет сохранённой серверной связи. Эта таблица показывает, что воронка исключила. ID с доставленным player_deleted исключены. Старые удаления без события неизвестны.',
 "SELECT count(*) AS observed_profiles,count(registered_at) AS confirmed_registrations,count(*)-count(registered_at) AS registration_unknown,to_timestamp_micros(min(first_seen)) AS first_observation FROM selected",164,12)
 add('intensity','Интенсивность игры по сегментам, %','100% — активные профиле-дни за 7 завершённых дней МСК. Включены посещения без публикаций. Фильтры общие; вместе с процентами смотреть размер выборки ниже.',
 """, player_days AS (SELECT v.player_id,v.day,CASE '${compare}' WHEN 'world' THEN s.world WHEN 'source' THEN s.registration_source WHEN 'surface' THEN s.launch_surface ELSE s.age_group END AS segment,count(p.post_id) AS posts FROM visits v JOIN selected s ON v.player_id=s.player_id CROSS JOIN clock c LEFT JOIN posts p ON p.player_id=v.player_id AND floor((p.posted_at+10800000000)/86400000000)=v.day WHERE v.day>=c.today-7 AND v.day<c.today GROUP BY v.player_id,v.day,segment), buckets AS (
 SELECT segment,CASE WHEN posts=0 THEN '0 постов' WHEN posts=1 THEN '1 пост' WHEN posts<=4 THEN '2–4 поста' WHEN posts<=9 THEN '5–9 постов' ELSE '10+ постов' END AS bucket,count(*) AS player_days FROM player_days GROUP BY segment,bucket), bases AS (SELECT segment,sum(player_days) AS base FROM buckets GROUP BY segment)
 SELECT b.segment,b.bucket,round(100.0*b.player_days/nullif(a.base,0),1) AS player_days_pct FROM buckets b JOIN bases a ON b.segment=a.segment ORDER BY b.segment,b.bucket""",176,16,'stacked',breakdown='bucket',values=['player_days_pct'])
 panels[-1]['config'].update({'unit':'percent','y_axis_min':0,'y_axis_max':100})
 add('engagement','Публикации и активное время · 7 полных дней','Время — foreground после взаимодействия; не время открытого окна. Среднее только по измеренным профиле-дням, measured_days показывает покрытие. Нули публикаций включены. Дни МСК, сегодня исключено.',
 """, player_days AS (SELECT v.player_id,v.day,CASE '${compare}' WHEN 'world' THEN s.world WHEN 'source' THEN s.registration_source WHEN 'surface' THEN s.launch_surface ELSE s.age_group END AS segment,count(p.post_id) AS posts FROM visits v JOIN selected s ON v.player_id=s.player_id CROSS JOIN clock c LEFT JOIN posts p ON p.player_id=v.player_id AND floor((p.posted_at+10800000000)/86400000000)=v.day WHERE v.day>=c.today-7 AND v.day<c.today GROUP BY v.player_id,v.day,segment), measured AS (
 SELECT player_id,floor((_timestamp+10800000000)/86400000000) AS day,sum(cast(prop_ms AS DOUBLE))/60000.0 AS minutes FROM raw WHERE event_name='active_time' GROUP BY player_id,day)
 SELECT p.segment,count(DISTINCT p.player_id) AS active_profiles,count(*) AS player_days,sum(p.posts) AS posts,round(1.0*sum(p.posts)/count(DISTINCT p.player_id),2) AS posts_per_profile,
 count(m.minutes) AS measured_days,round(avg(m.minutes),2) AS mean_active_minutes FROM player_days p LEFT JOIN measured m ON p.player_id=m.player_id AND p.day=m.day GROUP BY p.segment ORDER BY p.segment""",192,18)
 add('failed-posts','Ошибки генерации по дням · сервер','Подтверждённые сервером неуспешные генерации; повтор одного publication_id считается один раз. Не вычитаются из числа завершённых постов. Покрытие начинается с нового релиза.',
 "SELECT to_timestamp_micros(floor((r._timestamp+10800000000)/86400000000)*86400000000-10800000000) AS day_at,count(DISTINCT r.prop_id) AS failed_publications FROM raw r JOIN selected s ON r.player_id=s.player_id CROSS JOIN clock c WHERE r.event_name='post_failed' AND floor((r._timestamp+10800000000)/86400000000)>=c.today-cast('${days}' AS INT)+1 GROUP BY 1 ORDER BY 1",210,12,'bar')
 # Horizontal bars share the exact cohort with the numeric funnel table.
 funnel = next(p for p in panels if p['id']=='funnel')
 chart = sql_panel('funnel-bars','Прохождение: конверсия от регистрации, %',funnel['description'],funnel['queries'][0]['query'],'product',box(0,46,96,14,len(panels)+1),kind='h-bar',values=['conversion_pct'])
 chart['config'].update({'panel_time_enabled':True,'panel_time_range':{'type':'relative','relativeTimePeriod':'90d'},'unit':'percent','x_axis_min':0,'x_axis_max':100})
 panels.append(chart)
 return panels,variables

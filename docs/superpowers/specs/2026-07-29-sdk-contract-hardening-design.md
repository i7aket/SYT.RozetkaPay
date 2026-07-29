# SYT.RozetkaPay: приведение SDK в порядок после 1.0.0

Дата: 2026-07-29
Статус: согласовано, готово к декомпозиции в план реализации
Эпик: [EXP-229](https://experthub.youtrack.cloud/issue/EXP-229)

## 1. Контекст

`v1.0.0` выпущен 2026-07-29 05:15 UTC другой сессией. Проверено по живым источникам:

```text
GitHub tag v1.0.0        -> есть, аннотированный
GitHub Release v1.0.0    -> есть, с nupkg + snupkg + SHA256SUMS
Release NuGet run        -> 30424475848, success
nuget.org 1.0.0          -> опубликован, страница 200, в search index
```

Внешних потребителей пакета нет. Решение владельца: **делать правильный SDK, не оглядываясь
на обратную совместимость**. Semver-ограничения при выборе решений не учитываем; ломающие
изменения допустимы и предпочтительны там, где иначе останется неверный контракт.

Задача этого документа — зафиксировать, что именно не так, и разбить починку на работы.

## 2. База доказательств

Два независимых аудита одного и того же коммита `5aba447`.

**Аудит A (Claude).** Машинная сверка живой спеки `https://docs.rozetkapay.com/openapi.json`
против скомпилированной сборки через рефлексию: 285 модельных классов, 31 enum, 179
именованных схем спеки, с разворачиванием `$ref` и `allOf`, с учётом наследования.
Плюс исполняемые репро для транспортных гипотез.

**Аудит B (Codex).** Слепой независимый аудит по тому же исходному заданию, без доступа к
находкам аудита A. Отчёт: `AUDIT-CODEX.md` (не коммитится, хранится в артефактах сессии).

Сверка двух аудитов дала: 5 пересечений, 9 находок только у B, 1 находка только у A,
2 расхождения в оценке. Оба расхождения разрешены в пользу B (см. §7).

### Что проверено и дефекта не найдено

Это важно не меньше находок — не тратить на это работы:

- набор операций совпадает как множество: `59 paths`, `67 operations`, живая спека против
  закоммиченного снапшота, расхождений в `(method, path, operationId)` нет;
- проверка подписи вебхуков корректна: воспроизводит опубликованный SHA-1-протокол, строго
  декодирует canonical base64url, сравнение constant-time
  (`Security/RozetkaPayWebhookSignatureVerifier.cs`);
- тела запросов и ответов не попадают в логи; встроенный логгер `IHttpClientFactory`
  отключён через `RemoveAllLoggers()`, что закрывает утечку URI и заголовков;
- caller-controlled значения в path и query кодируются (`Services/RequestTargetEncoding.cs`);
- trust-all TLS callback отсутствует, платформенная валидация сертификатов не ослаблена;
- опубликованный пакет структурно корректен: nuspec, MIT, README, иконка, DLL и XML для
  обоих TFM; `.snupkg` содержит только PDB; Source Link указывает на точный коммит;
- уязвимых зависимостей `dotnet package list --vulnerable` не показывает;
- XML-документация полная: 365 экспортированных типов, 0 без документации;
- тесты зелёные: 1480 passed, 1 skipped, 0 failed на `net9.0` и `net10.0`;
- **ретрай создания платежа безопасен**: спека гарантирует «at most one success payment is
  allowed with same `external_id` within single login». Эта гарантия распространяется только
  на создание — см. работу 3.

## 3. Работы

25 позиций. Каждая — один тикет EXP и один PR. Каждый PR проходит независимое ревью Codex
перед мержем.

### Блокеры

**W1. `fix(security)`: запретить редиректы на authenticated-транспорте и non-TLS endpoint**

Authenticated-клиенту не выставлен `AllowAutoRedirect = false`
(`Extensions/ServiceCollectionExtensions.cs:266-278`) — в отличие от anonymous
decline-клиента, где он выставлен явно (`:284-297`). При `302` .NET снимает `Authorization`,
но **переносит** пользовательские заголовки. Проверено репро:

```text
redirected_header=X-ON-BEHALF-OF: on-behalf-secret
redirected_header=X-CUSTOMER-AUTH: customer-auth-secret
```

Отдельно: валидатор опций разрешает `http://`
(`Configuration/RozetkaPayOptionsValidator.cs:90-93`, `152-155`), и это зафиксировано тестом
как допустимый контракт (`tests/RozetkaPayOptionsTests.cs:590-601`).

Сделать: `AllowAutoRedirect = false` на всех authenticated-транспортах, 3xx возвращать как
ошибку. Разрешать только HTTPS — без opt-in для non-loopback. Для собственных
интеграционных тестов на `LoopbackWebApplication` предусмотреть явный
`AllowInsecureLoopbackTransport`, запрещённый для не-loopback адресов. Тест, фиксирующий
`http://` как валидный, переписать на обратное утверждение. Добавить regression-проверку,
что при `302` ни один секретный заголовок не уходит на второй origin.

**W2. `fix(json)`: сериализовать enum в токены живой спеки**

Общий сериализатор использует `JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)`
(`Services/BaseService.cs:1229-1245`). Атрибуты `JsonPropertyName` к членам enum не
применяются вообще. Фактические значения на проводе:

```text
CustomerCheckoutLocale.UK             -> "uk"        спека требует "UK"
AlternativePaymentProvider.LeaseLink  -> "lease_link" спека требует "leaselink"
PayPartsPaymentMode.Single            -> "single"     спека требует "hosted" | "direct"
```

Расходятся 8 из 21 совпавшего по имени enum. `PayPartsPaymentMode` — не тот enum целиком.
`ResponseCode` — 57 членов против 185 в живой схеме, и `ThreeDs*` уходит как `three_ds_*`
вместо `3ds_*`. `BatchPaymentMode` не содержит `hosted`. `OperationType` не содержит
`lookup`, `recurrent`. `SubscriptionCallbackType` содержит три выдуманных значения вместо
десяти dotted event names. Пример из README (`README.md:119-140`) отправляет невалидное
значение.

Сделать: перейти на `JsonStringEnumMemberName` (или per-enum конвертер), привести все члены
к живой схеме, **неверные члены удалить**, недостающие добавить. Контрактные тесты должны
проверять фактические wire-токены, а не только тип.

**W3. `fix(reliability)`: убрать скрытый fallback по `404` и сделать ретраи
operation-aware**

`PostAsyncWithFallback` ловит **любой** `404` и повторяет тот же POST на другом маршруте
(`Services/BaseService.cs:441-467`). Так устроены create в Alternative Payments
(`AlternativePaymentService.cs:71-97`), create/confirm/cancel/refund в PayParts
(`PayPartsService.cs:73-142`), обновление карты по умолчанию (`CustomerService.cs:229-240`).
`404` может означать отсутствующий ресурс, а не отсутствующий маршрут — тогда SDK скрывает
исходную ошибку и делает вторую финансовую мутацию на неподтверждённом endpoint.

Отдельно: `RetryPolicy.Standard` повторяет любой POST трижды. Для создания платежа это
безопасно (серверная дедупликация по `external_id`), для `confirm`, `refund`, `cancel`
такой гарантии в спеке нет.

Сделать: автоматический fallback удалить полностью. Ретраи разрешать по метаданным
операции — безопасные `GET` и документированно идемпотентные операции; для остальных
явный per-call opt-in.

**W4. `fix(api)`: частичные capture и cancel**

`CancelPaymentRequest` в SDK содержит только `external_id` и выдуманный `reason`, тогда как
спека объявляет `amount`, `currency`, `products`, `payload`, `callback_url`.
`ConfirmPaymentRequest` — не хватает `callback_url`, `currency`, `payload`, `products`.
То есть частичное списание и частичная отмена через SDK невозможны вообще.

Сделать: привести обе модели к спеке, `reason` удалить.

### Инфраструктура

**W5. `test(openapi)`: живая спека как CI-oracle**

Парити-тесты сверяются с закоммиченным снапшотом `docs/openapi.json`; живую спеку не тянет
никто. Именно поэтому весь дрейф из §3 прошёл при 1480 зелёных тестах, и релиз `1.0.0`
уехал, не заметив расхождения.

Сделать: job в CI, который скачивает живую спеку, делает семантический diff со снапшотом и
падает при расхождении. Валидация полного тела запроса против JSON Schema, а не выборочная
проверка пары полей. Точная сверка enum-токенов.

**W6. `fix(http)`: не мутировать `HttpClient` потребителя**

`Services/BaseService.cs:129-130` пишет `BaseAddress` и `Timeout` в переданный клиент.
Проверено репро:

```text
[before]     BaseAddress=https://consumer.example/  Timeout=00:00:05
[after ctor] BaseAddress=https://api.rozetkapay.com/ Timeout=00:00:30
[reuse]      InvalidOperationException: This instance has already started one or more
             requests. Properties can only be modified before sending the first request.
```

Сделать: строить абсолютные URI на запрос, таймаут держать через `CancellationTokenSource`,
не трогать состояние чужого клиента.

**W7. `perf(json)`: закешировать `JsonSerializerOptions`**

`Services/BaseService.cs:1229` возвращает новый экземпляр на каждый вызов; за один запрос
вызывается дважды. Замер на моделях этого SDK:

```text
fresh options per call : 2.340 ms/op
cached options         : 0.002 ms/op   (1298x)
```

Около 4.7 мс чистого CPU на каждый вызов API плюс полный кеш контрактов сериализации в
мусор. Сделать: `static readonly`.

**W8. `fix(time)`: не выдавать локальное время за UTC**

`Converters/FlexibleDateTimeConverter.cs:66-76` дописывает литерал `Z`, не вызывая
`ToUniversalTime()`. Unix-таймстемп возвращается с `DateTimeKind.Unspecified` (`:57-60`).
Проба в `Europe/Berlin`:

```text
local_input=2026-07-29T12:00:00+02:00   ->  serialized="2026-07-29T12:00:00.000Z"
```

Момент времени уезжает со сдвигом на offset. Существующий тест проверяет только уже-UTC
вход (`tests/CoreBehaviorCoverageTests.cs:183-198`).

Сделать: нормализовать `Local` в UTC, `Unspecified` в запросах отклонять. Публичную
поверхность перевести на `DateTimeOffset` — ломающее изменение допустимо.

### Контракт

Основание — машинная сверка: из 129 совпавших по имени схем 60 чистые, 48 с реальным
дрейфом (109 недостающих полей, 51 лишнее, 16 неверных `[Required]`), 17 требуют ручного
разбора (см. W18).

**W9. `fix(api)`: `CreateLookupRequest`** — пересечение со спекой равно нулю. Спека требует
`mode`; SDK шлёт только `external_id`. Не хватает `callback_url`, `currency`, `customer`,
`description`, `payload`, `result_url`.

**W10. `fix(api)`: `CreateRecurrentPaymentRequest`** — нет `confirm`, `delegate_api_key`,
`payload`, `subscription_id`, `unified_external_id`; лишние `currency`, `customer`,
`description`.

**W11. `fix(api)`: batch** — `CancelBatchPaymentRequest` шлёт `external_id` вместо
`batch_external_id`; `ConfirmBatchPaymentRequest` не имеет обязательного `currency` и несёт
лишний обязательный `external_id`; `CreateBatchPaymentRequest` не имеет `campaign_name`,
`checkout_ttl`, `result_url_fail`, `result_url_success`.

**W12. `fix(api)`: `CreatePaymentRequestDev`** — не хватает `result_url_success`,
`result_url_fail`, `subscription_id`, `use_custom_free_amount`. Отдельно разобрать, почему
компонент `CreatePaymentRequest` в спеке не referenced ни одной операцией, а операции
используют `CreatePaymentRequestDev`.

**W13. `fix(api)`: Alternative Payments** — сервис принимает
`CreateAlternativePaymentRequest`, где `description` и `customer` nullable, `products` нет,
зато есть посторонние `payment_method`, `return_url`, `payment_method_data`. Более близкий к
спеке `CreateAlternativePayment` в коде есть, но к сервису не подключён. Спека требует
`provider, external_id, amount, currency, description, customer, products`.

**W14. `fix(api)`: PayParts** — сервис принимает legacy `CreatePayPartsOrderRequest` с
`bank`, `merchant_id`, `success_url`, `failure_url` вместо требуемых спекой
`bank_name, mode, external_id, amount, currency, parts_count, description, customer`.
Правильный `CreatePayPartsOrder` не подключён и использует неверный enum режима. Плюс
`deliveries` и `RefundPPayRequest.products`.

**W15. `fix(api)`: подписки и планы** — `CreatePlanRequest` требует
`name, price, currency, frequency_type, frequency, duration_periods, start_date`; сервис
принимает старый `CreateSubscriptionPlanRequest` с `amount`, строковым `frequency`,
`trial_days`. `CreateSubscriptionRequest` требует `customer, plan_id, result_url,
start_date`; в SDK нет `plan_id` и `result_url`, `customer` и `start_date` nullable, а
обязательными объявлены старые `amount, currency, external_id, frequency`. Плюс
`callbacks`, `duration_periods`.

**W16. `fix(api)`: `SetDefaultCardRequest`** — спека требует `option_id` (uuid) и `type`
(enum `card`); SDK шлёт `card_id`.

**W17. `feat(api)`: `metadata` во все десять мест** — схема `Metadata` появилась в живой
спеке: `Dictionary<string,string>`, не более 10 записей, ключ до 30 символов, значение до
200. В SDK присутствует в трёх местах и неверным типом `Dictionary<string, object>`.
Это поле принадлежит целиком данной работе: в W11, W12 и W14 оно намеренно не упомянуто,
чтобы два PR не правили одно и то же поле.
Требуется в шести телах запросов (`CreatePaymentRequest`, `CreatePaymentRequestDev`,
`CreateBatchPaymentRequest`, `CreateRecurrentPaymentRequest`, `CreatePayPartsOrder`,
`CreateAlternativePayment`) и четырёх ответах (`PaymentOperationResult`,
`BatchPaymentOperationResult`, `PayPartsOperationResult`,
`AlternativePaymentOperationResult`). Тип везде привести к `string`, лимиты валидировать.

**W18. `fix(api)`: ручной разбор 17 схем с совпавшими именами** — автоматическая
классификация отнесла их к вероятным коллизиям имён (перекрытие полей менее трети).
Пример подтверждённой коллизии: спека `AlternativePaymentMethod` = `{blik, type}`, класс
SDK с тем же именем = `{code, is_active, logo_url, name}` — это разные сущности. Пройти все
17 и решить по каждой: коллизия имени или дефект моделирования. Найденные дефекты
оформляются отдельными тикетами.

**W19. `fix(api)`: остаток response-моделей** — `bin_country` в
`ApplePayResponsePaymentMethod`, `GooglePayResponsePaymentMethod`, `CCTokenResponsePaymentMethod`;
`saved_card`; `details`, `unified_external_id` в operation-результатах; и остальные
недостающие поля из отчёта сверки, не покрытые W9-W18.

### Замыкающие работы

**W20. `fix(validation)`: выровнять `[Required]` по спеке и включить валидацию**

В моделях 202 атрибута `[Required]`, вызовов `Validator.ValidateObject` или
`TryValidateObject` в `src/` — ноль. Атрибуты декоративны. При этом расставлены они не по
спеке: 16 расхождений в обе стороны. `blik_code`, `customer`, `products`, `platforms`,
`callback_url`, `phone`, `external_id` помечены обязательными, хотя спека их не требует;
реально обязательные `api_key`, `description`, `start_date`, `type`, `quantity`, `name`,
`price` — без атрибута.

Порядок внутри работы обязателен: сначала выровнять разметку по спеке, потом включить
валидацию перед сериализацией. В обратном порядке SDK начнёт отклонять валидные запросы.

**W21. `refactor(api)`: 16 маршрутов вне спеки**

Публичный интерфейс вызывает маршруты, которых нет в `live.paths`:

```text
/api/alternative-payments/v1/{methods,new,operations}
/api/merchant/v1/{commission-rates,settings}
/api/payments/v1/{list,p2p/confirm}
/api/payouts/v1/{balance,list,new}
/api/payparts/v1/{banks,new,operations}
/api/payments/v1/payparts/{confirm,cancel,refund}
```

Сделать: запросить у RozetkaPay подтверждение по каждому. Неподтверждённые удалить —
ломающее изменение допустимо. Подтверждённые задокументировать как расширение вне спеки.

**W22. `build(api)`: baseline публичной поверхности** — ни `EnablePackageValidation`, ни
`ApiCompat`, ни `PublicAPI.Shipped` не настроены (`SYT.RozetkaPay.csproj`,
`.github/workflows/ci.yml:49-82`). Случайное удаление, переименование или смена nullability
публичного члена проходит зелёным. Зафиксировать baseline против той поверхности, которая
получится после W1-W21 — не против `1.0.0`, поскольку ломающие изменения запланированы.

**W23. `fix(release)`: сверять опубликованный пакет с локальным артефактом** — workflow
пушит с `--skip-duplicate` и создаёт GitHub Release из локальных артефактов, не сверяя их с
тем, что реально лежит на NuGet (`.github/workflows/release.yml:208-248`). При сценарии
«push прошёл, GitHub Release упал» повторный запуск пропустит существующий пакет и выложит
на GitHub другие биты под той же версией, оставшись зелёным. Добавить: после push дождаться
появления пакета, скачать, сверить SHA-256 с локальным артефактом, и только затем создавать
GitHub Release.

**W24. `chore(deps)`: обновить зависимости** — `Microsoft.Extensions.*` закреплены на
`9.0.5` при доступных `9.0.18`.

**W25. `docs(contract)`: привести документацию к доказанному контракту**

Выполняется последней. README утверждает проверенный body-parity и
`59/59`, `67 operations` (`README.md:3-20`, `42-67`) — это опровергается находками W9-W19.
`API_COMPATIBILITY.md` называет SDK `0.1.0-alpha.1` и утверждает, что снапшот побайтово
совпадает с живым документом, что опровергается diff'ом. Инструкция по установке всё ещё
использует `--prerelease` (`README.md:79-83`). Плюс отсутствует заявление об официальном
или неофициальном статусе SDK и атрибуция торговой марки.

Документация обновляется **после** кодовых работ и описывает только доказанный контракт.

## 4. Соответствие работ и тикетов

Эпик: [EXP-229](https://experthub.youtrack.cloud/issue/EXP-229). Все тикеты заведены как подзадачи.

| Работа | Тикет | Работа | Тикет |
|---|---|---|---|
| W1 | [EXP-383](https://experthub.youtrack.cloud/issue/EXP-383) | W14 | [EXP-396](https://experthub.youtrack.cloud/issue/EXP-396) |
| W2 | [EXP-384](https://experthub.youtrack.cloud/issue/EXP-384) | W15 | [EXP-397](https://experthub.youtrack.cloud/issue/EXP-397) |
| W3 | [EXP-385](https://experthub.youtrack.cloud/issue/EXP-385) | W16 | [EXP-398](https://experthub.youtrack.cloud/issue/EXP-398) |
| W4 | [EXP-386](https://experthub.youtrack.cloud/issue/EXP-386) | W17 | [EXP-399](https://experthub.youtrack.cloud/issue/EXP-399) |
| W5 | [EXP-387](https://experthub.youtrack.cloud/issue/EXP-387) | W18 | [EXP-400](https://experthub.youtrack.cloud/issue/EXP-400) |
| W6 | [EXP-388](https://experthub.youtrack.cloud/issue/EXP-388) | W19 | [EXP-401](https://experthub.youtrack.cloud/issue/EXP-401) |
| W7 | [EXP-389](https://experthub.youtrack.cloud/issue/EXP-389) | W20 | [EXP-402](https://experthub.youtrack.cloud/issue/EXP-402) |
| W8 | [EXP-390](https://experthub.youtrack.cloud/issue/EXP-390) | W21 | [EXP-403](https://experthub.youtrack.cloud/issue/EXP-403) |
| W9 | [EXP-391](https://experthub.youtrack.cloud/issue/EXP-391) | W22 | [EXP-404](https://experthub.youtrack.cloud/issue/EXP-404) |
| W10 | [EXP-392](https://experthub.youtrack.cloud/issue/EXP-392) | W23 | [EXP-405](https://experthub.youtrack.cloud/issue/EXP-405) |
| W11 | [EXP-393](https://experthub.youtrack.cloud/issue/EXP-393) | W24 | [EXP-406](https://experthub.youtrack.cloud/issue/EXP-406) |
| W12 | [EXP-394](https://experthub.youtrack.cloud/issue/EXP-394) | W25 | [EXP-407](https://experthub.youtrack.cloud/issue/EXP-407) |
| W13 | [EXP-395](https://experthub.youtrack.cloud/issue/EXP-395) | | |

## 5. Порядок

```
W1 W2 W3 W4          блокеры, строго первыми
  -> W5              drift-детектор: без него следующий дрейф снова пройдёт незамеченным
  -> W6 W7 W8        транспорт и корректность, независимы друг от друга
  -> W9 .. W19       контракт; W18 может породить новые тикеты
  -> W20             только после W9-W19, иначе валидация отвергнет валидное
  -> W21 W22 W23 W24 поверхность, гейты, зависимости
  -> W25             документация, строго последней
```

## 6. Определение готовности

Работа считается завершённой, когда:

- все 25 PR смержены, каждый прошёл независимое ревью Codex;
- CI-job живой сверки со спекой зелёный и падает на искусственно внесённом расхождении;
- контрактные тесты проверяют фактические wire-токены enum и полное тело запроса;
- прогон против sandbox с реальными ключами выполнен, а не пропущен;
- `dotnet test` зелёный на обоих TFM;
- README и `API_COMPATIBILITY.md` описывают только то, что доказано тестами.

## 7. Разрешённые расхождения аудитов

**NuGet 1.0.0.** Аудит A зафиксировал отсутствие пакета на nuget.org через два часа после
успешного push (404 на странице версии, индекс только с alpha). Аудит B, выполненный позже,
увидел пакет опубликованным. Перепроверка подтвердила версию B: это была задержка
индексации. Вывод A ошибочен, работа по этому поводу не заводится.

**Ретраи мутирующих операций.** Аудит A снял подозрение, опираясь на гарантию спеки «at most
one success payment per `external_id`». Аудит B поставил `major`. Оба верны частично:
гарантия покрывает только создание платежа и не распространяется на `confirm`, `refund`,
`cancel` и на путь fallback по `404`. Находка принята с суженной областью — см. W3.

## 8. Открытые вопросы

- Подтверждение RozetkaPay по 16 маршрутам вне спеки (W21) требует обращения к провайдеру;
  до ответа работа не может быть закрыта.
- Заявление об официальном или неофициальном статусе SDK и права на имя пакета — решение
  владельца, не техническое.
- Прогон против sandbox требует реальных merchant-ключей; сейчас соответствующий тест
  пропускается молча (`SandboxFactAttribute`).

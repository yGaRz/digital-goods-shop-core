# API docs (локальный compose)

**Канон ручных запросов.** Меняешь REST (путь, метод, тело, код ответа, новый сервис в compose) — в **том же изменении** обновляй этот файл: описание, копируемое тело, ссылка с хоста. Параллельно: [АРХИТЕКТУРА.md](../АРХИТЕКТУРА.md) §4 (контракт).

Стек: [deploy/docker-compose.yml](../deploy/docker-compose.yml).

Документ растёт вместе с этапами. Сейчас — **E0–E1c** (Shop + Buyer + Bank/эскроу).

## Поднять

```powershell
docker compose -f deploy/docker-compose.yml up --build -d
```

## Ссылки compose (с хоста)

| Сервис | URL / адрес | Зачем |
| --- | --- | --- |
| Shop API | [http://localhost:8080](http://localhost:8080) | заказы, health, каталог |
| Shop health | [http://localhost:8080/health](http://localhost:8080/health) | Postgres доступен → 200 |
| Buyer API | [http://localhost:8081](http://localhost:8081) | одна покупка → заказ в Shop |
| Buyer health | [http://localhost:8081/health](http://localhost:8081/health) | Shop доступен → 200 |
| Bank API | [http://localhost:8082](http://localhost:8082) | эмуляция эквайринга → webhook Shop |
| Bank health | [http://localhost:8082/health](http://localhost:8082/health) | Shop доступен → 200 |
| Каталог | [http://localhost:8080/products](http://localhost:8080/products) | 12 SKU после seed |
| Kibana | [http://localhost:5601](http://localhost:5601) | Discover, Data View `shop-*` |
| Elasticsearch | [http://localhost:9200](http://localhost:9200) | индексы `shop-*` |
| Logstash TCP | `localhost:5000` | приём JSON-логов (не браузер) |
| PostgreSQL | `localhost:5433` | user/password/db: `shop` (в контейнере порт 5432) |

---

## Эндпоинты Shop

### Health

Проверка: процесс жив и дотягивается до Postgres. Без БД → **503**.

- Метод: `GET`
- URL: [http://localhost:8080/health](http://localhost:8080/health)
- Тело: нет

```http
GET http://localhost:8080/health
```

Ожидание: `200` и текст `Healthy`.

---

### Список товаров (seed)

- Метод: `GET`
- URL: [http://localhost:8080/products](http://localhost:8080/products)
- Тело: нет

```http
GET http://localhost:8080/products
```

Ожидание: JSON-массив из **12** SKU, в том числе `STEAM-TOPUP-500` с `price: 500`.

---

### Создать заказ

Цена **копируется** из каталога в заказ (`amount`). Статус `created`. Банка ещё нет.

- Метод: `POST`
- URL: [http://localhost:8080/orders](http://localhost:8080/orders)
- Header: `Content-Type: application/json`

Тело:

```json
{
  "sku": "STEAM-TOPUP-500"
}
```

Другой SKU из каталога:

```json
{
  "sku": "KEY-CS2-PRIME"
}
```

```http
POST http://localhost:8080/orders
Content-Type: application/json

{
  "sku": "STEAM-TOPUP-500"
}
```

Ожидание: `201 Created`, например:

```json
{
  "id": "ord_…",
  "sku": "STEAM-TOPUP-500",
  "amount": 500.00,
  "currency": "RUB",
  "status": "created",
  "code": null,
  "createdAt": "…"
}
```

Неизвестный SKU → `404` `{ "error": "unknown_sku", … }` (не 500).

---

### Получить заказ

- Метод: `GET`
- URL: `http://localhost:8080/orders/{id}` — подставьте `id` из ответа create / purchase.

```http
GET http://localhost:8080/orders/ord_06defdabc65340839ea4ff4eda58f339
```

Ожидание: `200` и тот же снимок заказа. Нет такого id → `404`.

---

## Эндпоинты Buyer

Тонкий клиент **без своей БД**. Один `POST /purchases` = один `POST /orders` в Shop. `/storm` — только на E4b.

### Health

Проверка: процесс жив и дотягивается до Shop `/health`. Shop недоступен → **503**.

- Метод: `GET`
- URL: [http://localhost:8081/health](http://localhost:8081/health)
- Тело: нет

```http
GET http://localhost:8081/health
```

Ожидание: `200` и текст `Healthy`.

---

### Купить (один покупатель)

Проксирует в Shop `POST /orders`. В ответе — `orderId` (это `orders.id` магазина). Лог Buyer: `service=buyer` + `order_id`.

- Метод: `POST`
- URL: [http://localhost:8081/purchases](http://localhost:8081/purchases)
- Header: `Content-Type: application/json`

Тело:

```json
{
  "sku": "STEAM-TOPUP-500"
}
```

```http
POST http://localhost:8081/purchases
Content-Type: application/json

{
  "sku": "STEAM-TOPUP-500"
}
```

Ожидание: `201 Created`, например:

```json
{
  "orderId": "ord_…",
  "sku": "STEAM-TOPUP-500",
  "amount": 500.00,
  "currency": "RUB",
  "status": "created"
}
```

Неизвестный SKU → `404` `{ "error": "unknown_sku", … }`. Пустой `sku` → `400`.

Дальше проверка заказа: `GET http://localhost:8080/orders/{orderId}`.

---

## Эндпоинты Shop — оплата (вебхук)

Контракт как в ТЗ. Повтор того же `event_id` → **200**, без второй проводки. Чужой `amount` → **200**, статус заказа не `paid`, в логе `reason=amount_mismatch`. Заказа ещё нет → **200**, событие сохранено (`order_missing`, дожим на E2).

### Принять вебхук

- Метод: `POST`
- URL: [http://localhost:8080/webhook/payment](http://localhost:8080/webhook/payment)
- Header: `Content-Type: application/json`

Тело (happy path, amount = снимок заказа):

```json
{
  "event_id": "evt_a1b2c3",
  "order_id": "ord_…",
  "status": "paid",
  "amount": 500,
  "currency": "RUB",
  "created_at": "2026-09-11T12:00:00Z"
}
```

Оплата не прошла (S2):

```json
{
  "event_id": "evt_fail_1",
  "order_id": "ord_…",
  "status": "failed",
  "amount": 500,
  "currency": "RUB",
  "created_at": "2026-09-11T12:00:00Z"
}
```

S16 (сумма не та) — тот же `paid`, но `"amount": 1` при заказе на 1290.

```http
POST http://localhost:8080/webhook/payment
Content-Type: application/json

{
  "event_id": "evt_a1b2c3",
  "order_id": "ord_…",
  "status": "paid",
  "amount": 500,
  "currency": "RUB",
  "created_at": "2026-09-11T12:00:00Z"
}
```

Ожидание `paid`: заказ `status: paid`, ledger `bank_asset`/`escrow` = amount, `seller_payable` = 0, ключа нет.  
Ожидание `failed`: `payment_failed`, ledger пуст.  
Ожидание mismatch: заказ остаётся `created`, `outcome: amount_mismatch`.

---

## Эндпоинты Bank

Bank **не** пишет ledger. Генерирует `event_id` и шлёт вебхук в Shop. Без `amount` в теле — берёт сумму из `GET /orders/{id}`.

### Health

- Метод: `GET`
- URL: [http://localhost:8082/health](http://localhost:8082/health)

```http
GET http://localhost:8082/health
```

Ожидание: `200` / `Healthy`.

---

### Оплатить заказ

- Метод: `POST`
- URL: [http://localhost:8082/payments](http://localhost:8082/payments)
- Header: `Content-Type: application/json`

Тело (сумма с заказа):

```json
{
  "orderId": "ord_…",
  "status": "paid"
}
```

S2:

```json
{
  "orderId": "ord_…",
  "status": "failed"
}
```

S16 (явный чужой amount):

```json
{
  "orderId": "ord_…",
  "status": "paid",
  "amount": 1,
  "currency": "RUB"
}
```

```http
POST http://localhost:8082/payments
Content-Type: application/json

{
  "orderId": "ord_…",
  "status": "paid"
}
```

Ожидание: `200`, в ответе `eventId`, Shop обработал вебхук.

---

## Минимальный ручной сценарий E1c

1. `POST /orders` или Buyer `POST /purchases` → `orderId`, `created`.  
2. [Health bank](http://localhost:8082/health) → 200.  
3. `POST /payments` `{ orderId, status: "paid" }` → заказ `paid`.  
4. SQL / лог: `bank_asset` = `escrow` = amount, payable = 0.  
5. Другой заказ: `status: "failed"` → `payment_failed`, ledger пуст (S2).  
6. Заказ `KEY-CS2-PRIME` + payment с `"amount": 1` → заказ не `paid` (S16).

Логи:

```powershell
docker compose -f deploy/docker-compose.yml logs -f bank shop
```

---

## Позже

Seller A/B `:8083`/`:8084`, выдача, storm — с E1d+.

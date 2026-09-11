# API docs (локальный compose)

**Канон ручных запросов.** Меняешь REST (путь, метод, тело, код ответа, новый сервис в compose) — в **том же изменении** обновляй этот файл: описание, копируемое тело, ссылка с хоста. Параллельно: [АРХИТЕКТУРА.md](../АРХИТЕКТУРА.md) §4 (контракт).

Стек: [deploy/docker-compose.yml](../deploy/docker-compose.yml).

Документ растёт вместе с этапами. Сейчас — **E0 + E1a + E1b** (Shop + тонкий Buyer).

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

## Минимальный ручной сценарий E1b

1. [Health shop](http://localhost:8080/health) → 200.  
2. [Health buyer](http://localhost:8081/health) → 200.  
3. `POST /purchases` с телом выше → `created`, `orderId`.  
4. `GET /orders/{orderId}` на Shop → тот же заказ.  
5. (опционально) Kibana: фильтр `service: buyer OR service: shop` и поле `order_id`.

Логи без Kibana:

```powershell
docker compose -f deploy/docker-compose.yml logs -f buyer shop
```

---

## Позже

Bank `:8082`, Seller A/B `:8083`/`:8084`, webhook, storm — появятся на E1c+. Этот файл дополним телами и ссылками.

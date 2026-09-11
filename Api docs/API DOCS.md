# API docs (локальный compose)

**Канон ручных запросов.** Меняешь REST (путь, метод, тело, код ответа, новый сервис в compose) — в **том же изменении** обновляй этот файл: описание, копируемое тело, ссылка с хоста. Параллельно: [АРХИТЕКТУРА.md](../АРХИТЕКТУРА.md) §4 (контракт) и при необходимости JSON в [postman/](../postman/).

Стек: [deploy/docker-compose.yml](../deploy/docker-compose.yml).  
Коллекция: [DigitalGoodsShop.postman_collection.json](../postman/DigitalGoodsShop.postman_collection.json).  
Environment: [local.postman_environment.json](../postman/local.postman_environment.json) (`shopUrl`, `orderId`).

Документ растёт вместе с этапами. Сейчас — **E0 + E1a** (только Shop).

## Поднять

```powershell
docker compose -f deploy/docker-compose.yml up --build -d
```

## Ссылки compose (с хоста)

| Сервис | URL / адрес | Зачем |
| --- | --- | --- |
| Shop API | [http://localhost:8080](http://localhost:8080) | заказы, health, каталог |
| Shop health | [http://localhost:8080/health](http://localhost:8080/health) | Postgres доступен → 200 |
| Каталог | [http://localhost:8080/products](http://localhost:8080/products) | 12 SKU после seed |
| Kibana | [http://localhost:5601](http://localhost:5601) | Discover, Data View `shop-*` |
| Elasticsearch | [http://localhost:9200](http://localhost:9200) | индексы `shop-*` |
| Logstash TCP | `localhost:5000` | приём JSON-логов (не браузер) |
| PostgreSQL | `localhost:5433` | user/password/db: `shop` (в контейнере порт 5432) |

Переменные Postman:

| Ключ | Значение по умолчанию |
| --- | --- |
| `shopUrl` | `http://localhost:8080` |
| `orderId` | заполняется тестом после Create order |

## Импорт Postman

1. Import → [коллекция](../postman/DigitalGoodsShop.postman_collection.json) и [environment](../postman/local.postman_environment.json).
2. В правом верхнем углу выбрать environment **local**.
3. Папки: **Health** → **Catalog** → **Orders**.

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

```powershell
curl.exe http://localhost:8080/health
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

```powershell
curl.exe http://localhost:8080/products
```

Ожидание: JSON-массив из **12** SKU, в том числе `STEAM-TOPUP-500` с `price: 500`.

---

### Создать заказ

Цена **копируется** из каталога в заказ (`amount`). Статус `created`. Банка ещё нет.

- Метод: `POST`
- URL: [http://localhost:8080/orders](http://localhost:8080/orders)
- Header: `Content-Type: application/json`

Тело (happy path):

```json
{
  "sku": "STEAM-TOPUP-500"
}
```

Другие SKU из каталога — та же форма, например:

```json
{
  "sku": "KEY-CS2-PRIME"
}
```

```powershell
curl.exe -X POST http://localhost:8080/orders `
  -H "Content-Type: application/json" `
  -d "{\"sku\":\"STEAM-TOPUP-500\"}"
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

В Postman после успешного Create в environment пишется `orderId`.

---

### Получить заказ

- Метод: `GET`
- URL: `http://localhost:8080/orders/{id}`  
  Пример после create: подставьте свой `id` или `{{orderId}}` в Postman.

```http
GET http://localhost:8080/orders/ord_06defdabc65340839ea4ff4eda58f339
```

```powershell
curl.exe http://localhost:8080/orders/ВАШ_ORDER_ID
```

Ожидание: `200` и тот же снимок заказа. Нет такого id → `404`.

---

## Минимальный ручной сценарий E1a

1. [Health](http://localhost:8080/health) → 200.  
2. [Products](http://localhost:8080/products) → 12 строк.  
3. `POST /orders` с телом `STEAM-TOPUP-500` → `created`, `amount: 500`.  
4. `GET /orders/{id}` → тот же заказ.  
5. (опционально) Kibana: [http://localhost:5601/app/discover](http://localhost:5601/app/discover), Data View `shop-*`, фильтр `service: shop`.

Логи контейнера без Kibana:

```powershell
docker compose -f deploy/docker-compose.yml logs -f shop
```

---

## Позже (ещё нет в коллекции)

Buyer `:8081`, Bank `:8082`, Seller A/B `:8083`/`:8084`, webhook, storm — появятся на E1b+. Этот файл дополним вместе с коллекцией.

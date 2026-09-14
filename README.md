# Ядро магазина цифровых товаров

Тестовое задание на Backend-разработчика: платежи, заказы, каталог, интеграции с поставщиками и автоматическая выдача цифровых товаров.

- ТЗ: [ЗАДАНИЕ.md](ЗАДАНИЕ.md)
- Глоссарий: [обучение/глоссарий.md](обучение/глоссарий.md)
- Обучение (главное): [обучение/README.md](обучение/README.md) — один покупатель → несколько → шторм
- Сценарии и поломки: [обучение/сценарии.md](обучение/сценарии.md)
- Архитектура, домен, задачи: [АРХИТЕКТУРА.md](АРХИТЕКТУРА.md)
- Порядок этапов: [ПЛАН.md](ПЛАН.md)

## Каркас E0 (только Shop)

Нужны Docker Desktop и .NET 9 SDK. Elasticsearch в compose с heap ~1 GB — на ноуте не поднимайте параллельно тяжёлые VM.

```powershell
dotnet build DigitalGoodsShop.sln
docker compose -f deploy/docker-compose.yml up --build
```

- Shop health: http://localhost:8080/health
- Каталог: http://localhost:8080/products (12 SKU после E1a)
- Заказ: `POST /orders` `{ "sku": "STEAM-TOPUP-500" }` → `created`
- Оплата (E1c): Bank http://localhost:8082/payments → заказ `paid`, эскроу (без ключа)
- PostgreSQL (DBeaver / локальный `dotnet run`): `localhost:5433`, БД/user/password `shop` (внутри compose сервис слушает `:5432`)
- Kibana: http://localhost:5601 — Data View `shop-*`, фильтр `service: shop`
- API docs (тела + ссылки): [Api docs/API DOCS.md](Api%20docs/API%20DOCS.md) — обновлять при смене эндпоинтов
- Тесты: `dotnet test DigitalGoodsShop.sln`

Полная инструкция «с нуля за 15 минут» — этап E7.

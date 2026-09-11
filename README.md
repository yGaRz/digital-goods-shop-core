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
- Kibana: http://localhost:5601 — Data View `shop-*`, фильтр `service: shop`
- Postman: импорт `postman/DigitalGoodsShop.postman_collection.json` и `postman/local.postman_environment.json` (`shopUrl`)

Полная инструкция «с нуля за 15 минут» — этап E7.

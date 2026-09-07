# E5. ELK и Grafana: увидеть инвариант

**Задачи:** T5.1–T5.3 · **~4 ч** · [S13](../сценарии.md#s13-sold-out-не-ошибка-инфраструктуры), [S14](../сценарии.md#s14-цепочка-логов-по-order_id)

## Зачем это учим

У вас был Graylog + Mongo + OpenSearch. Здесь цель — **тот же навык на ELK**: собрать инцидент по `order_id` и не путать бизнес-отказ с падением.

Метрики (Grafana) отвечают «сколько». Логи (Kibana) — «почему этот заказ».

## Что должно щёлкнуть

- Дашборд, где sold out = 5xx, **врёт** и учит плохим алертам.
- Корреляция: без `event_id`/`request_id` S6 не доказать.
- Index template сейчас избавит от «поле то keyword то text» на гонках.

## Сделать

Template `shop-*`, saved search, Grafana: статусы, keys_available, 2xx/409/5xx, ledger_imbalance. Прогнать S9 и смотреть глазами, не SQL.

## Проверить

S14 на одном happy path. Повторить S6 и найти оба issue. S13: error rate не следует за out_of_stock.

## Разобрать

- P16 OOS как Error  
- P17 логи без корреляции  
- Сравнение с Graylog: stream vs index, pipeline Logstash vs extractors  

Вопросы себе:

1. Какой запрос в Kibana докажет S3 (50 webhook, 1 issue)?
2. Какой panel в Grafana загорится при P2 (settlement на paid)?
3. Что алертить: imbalance ≠ 0 всегда; out_of_stock — порог, не page on-call?

## Антипаттерн ИИ

Тянуть весь ELK-кластер на 3 ноды. Для учёбы хватает однонодового ES.

## Дальше

[E6. Каталог](E6-каталог.md)

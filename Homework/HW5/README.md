# HW5 — ETag и optimistic concurrency

Задание выполнено на базе REST API магазина из HW4.

Для `Product` поле `Version` используется как версия ресурса и concurrency token
в SQLite. ETag имеет вид `"product-<id>-v<version>"` и меняется после каждого
изменения товара, включая изменение остатка при создании заказа.

## Запуск

```bash
cd Homework/HW5
dotnet run --urls http://localhost:5085
```

## Проверка через curl

Получить товар и ETag:

```bash
curl -i http://localhost:5085/api/products/<id>
```

Повторный GET с тем же ETag возвращает `304 Not Modified`:

```bash
curl -i http://localhost:5085/api/products/<id> \
  -H 'If-None-Match: "product-<id>-v1"'
```

Изменение без `If-Match` возвращает `428 Precondition Required`:

```bash
curl -i -X PUT http://localhost:5085/api/products/<id> \
  -H 'Content-Type: application/json' \
  -d '{"name":"Новая клавиатура","price":5000,"stock":9}'
```

Изменение со свежим ETag возвращает `200 OK` и новый ETag. Повторное
изменение со старым ETag возвращает `412 Precondition Failed`:

```bash
curl -i -X PUT http://localhost:5085/api/products/<id> \
  -H 'Content-Type: application/json' \
  -H 'If-Match: "product-<id>-v1"' \
  -d '{"name":"Новая клавиатура","price":5000,"stock":9}'
```

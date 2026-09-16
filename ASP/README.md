# Планировщик задач: что добавлено

Домашнее задание выполнено в проекте `ASP` на основе исходной архитектуры API продуктов.

## Архитектура

```text
HTTP endpoint в Program.cs
        ↓
ITaskService / TaskService
        ↓
ITaskRepository / TaskRepository
        ↓
TaskEntity
```

- `TaskEntity.cs` — внутренняя модель задачи.
- `TaskRequest.cs` — входные данные для создания и изменения задачи.
- `TaskResponseV1.cs` — базовый ответ версии `v1`.
- `TaskResponseV2.cs` — расширенный ответ версии `v2`.
- `ITaskRepository.cs` и `TaskRepository.cs` — хранение задач в памяти и soft delete.
- `ITaskService.cs` и `TaskService.cs` — бизнес-логика и преобразование DTO.
- `TaskRequestValidator.cs` — проверка обязательного названия и длины полей.
- `MappingProfile.cs` — настройка AutoMapper.
- `Program.cs` — регистрация зависимостей и HTTP-маршруты.

## Версионирование

Версия `v1` возвращает базовые поля:

- `id`;
- `title`;
- `description`;
- `dueDate`;
- `createdAt`.

Версия `v2` возвращает все поля `v1` и дополнительно два поля:

- `priority` — приоритет задачи;
- `status` — состояние задачи.

Для новых задач используются значения по умолчанию `Medium` и `Planned`.

## Маршруты

```text
GET     /api/v1/tasks
GET     /api/v1/tasks/{id}
POST    /api/v1/tasks
PUT     /api/v1/tasks/{id}

GET     /api/v2/tasks
GET     /api/v2/tasks/{id}
POST    /api/v2/tasks
PUT     /api/v2/tasks/{id}
DELETE  /api/v2/tasks/{id}
```

## Запуск и демонстрация

Из корня репозитория:

```bash
dotnet run --project ASP/Api.csproj
```

После запуска открыть Swagger:

```text
http://localhost:5187/swagger
```

Данные хранятся в памяти приложения, поэтому после перезапуска список задач очищается. Отдельная база данных для демонстрации не требуется.
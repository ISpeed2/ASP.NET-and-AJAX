namespace MyApi;

public class TaskRepository : ITaskRepository
{
    private readonly List<TaskEntity> _tasks = new();

    public Task<IEnumerable<TaskEntity>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<TaskEntity>>(
            _tasks.Where(task => task.DeletedAt == null).ToList());
    }

    public Task<TaskEntity?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(_tasks.FirstOrDefault(task =>
            task.Id == id && task.DeletedAt == null));
    }

    public Task<TaskEntity> CreateAsync(TaskEntity entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.DeletedAt = null;
        _tasks.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<TaskEntity?> UpdateAsync(TaskEntity entity)
    {
        var existingTask = _tasks.FirstOrDefault(task =>
            task.Id == entity.Id && task.DeletedAt == null);

        if (existingTask == null)
        {
            return Task.FromResult<TaskEntity?>(null);
        }

        existingTask.Title = entity.Title;
        existingTask.Description = entity.Description;
        existingTask.DueDate = entity.DueDate;
        existingTask.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult<TaskEntity?>(existingTask);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var task = _tasks.FirstOrDefault(item =>
            item.Id == id && item.DeletedAt == null);

        if (task == null)
        {
            return Task.FromResult(false);
        }

        task.DeletedAt = DateTime.UtcNow;
        task.UpdatedAt = task.DeletedAt;
        return Task.FromResult(true);
    }
}
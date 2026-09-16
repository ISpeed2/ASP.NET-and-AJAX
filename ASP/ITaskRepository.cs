namespace MyApi;

public interface ITaskRepository
{
    Task<IEnumerable<TaskEntity>> GetAllAsync();
    Task<TaskEntity?> GetByIdAsync(Guid id);
    Task<TaskEntity> CreateAsync(TaskEntity entity);
    Task<TaskEntity?> UpdateAsync(TaskEntity entity);
    Task<bool> DeleteAsync(Guid id);
}
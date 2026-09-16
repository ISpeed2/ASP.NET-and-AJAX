namespace MyApi;

public interface ITaskService
{
    Task<IEnumerable<TaskResponseV1>> GetAllV1Async();
    Task<TaskResponseV1?> GetByIdV1Async(Guid id);
    Task<TaskResponseV1> CreateV1Async(TaskRequest request);
    Task<TaskResponseV1?> UpdateV1Async(Guid id, TaskRequest request);

    Task<IEnumerable<TaskResponseV2>> GetAllV2Async();
    Task<TaskResponseV2?> GetByIdV2Async(Guid id);
    Task<TaskResponseV2> CreateV2Async(TaskRequest request);
    Task<TaskResponseV2?> UpdateV2Async(Guid id, TaskRequest request);
    Task<bool> DeleteAsync(Guid id);
}
using AutoMapper;

namespace MyApi;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly IMapper _mapper;

    public TaskService(ITaskRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<TaskResponseV1>> GetAllV1Async()
    {
        return _mapper.Map<IEnumerable<TaskResponseV1>>(await _repository.GetAllAsync());
    }

    public async Task<TaskResponseV1?> GetByIdV1Async(Guid id)
    {
        return _mapper.Map<TaskResponseV1?>(await _repository.GetByIdAsync(id));
    }

    public async Task<TaskResponseV1> CreateV1Async(TaskRequest request)
    {
        var entity = _mapper.Map<TaskEntity>(request);
        return _mapper.Map<TaskResponseV1>(await _repository.CreateAsync(entity));
    }

    public async Task<TaskResponseV1?> UpdateV1Async(Guid id, TaskRequest request)
    {
        var entity = _mapper.Map<TaskEntity>(request);
        entity.Id = id;
        return _mapper.Map<TaskResponseV1?>(await _repository.UpdateAsync(entity));
    }

    public async Task<IEnumerable<TaskResponseV2>> GetAllV2Async()
    {
        return _mapper.Map<IEnumerable<TaskResponseV2>>(await _repository.GetAllAsync());
    }

    public async Task<TaskResponseV2?> GetByIdV2Async(Guid id)
    {
        return _mapper.Map<TaskResponseV2?>(await _repository.GetByIdAsync(id));
    }

    public async Task<TaskResponseV2> CreateV2Async(TaskRequest request)
    {
        var entity = _mapper.Map<TaskEntity>(request);
        return _mapper.Map<TaskResponseV2>(await _repository.CreateAsync(entity));
    }

    public async Task<TaskResponseV2?> UpdateV2Async(Guid id, TaskRequest request)
    {
        var entity = _mapper.Map<TaskEntity>(request);
        entity.Id = id;
        return _mapper.Map<TaskResponseV2?>(await _repository.UpdateAsync(entity));
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        return _repository.DeleteAsync(id);
    }
}
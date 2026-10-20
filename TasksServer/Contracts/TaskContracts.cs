namespace TasksServer.Contracts;

public record CreateTaskRequest(string Title);

public record ToggleTaskResponse(bool IsCompleted);
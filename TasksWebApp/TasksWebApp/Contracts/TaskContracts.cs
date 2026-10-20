namespace TasksWebApp.Contracts;

public record CreateTaskRequest(string Title);
public record ToggleTaskResponse(bool IsCompleted);
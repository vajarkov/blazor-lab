namespace Tasks.Contracts;

public record TaskItemDto(int Id, string Title, bool IsCompleted);

public record CreateTaskRequest(string Title);

public record ToggleTaskResponse(bool IsCompleted);
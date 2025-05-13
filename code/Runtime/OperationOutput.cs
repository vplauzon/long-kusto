namespace Runtime
{
    public record ProcedureOutput<T>(string OperationId, Task<T> CompletionTask);
}
# AsyncLock.AcquireAsync method

Tries to acquire the lock asynchronously, and returns a task that completes when the lock has been acquired. The returned task contains a releaser that releases the lock when disposed. This is not a reentrant operation.

```csharp
public virtual Task<Releaser> AcquireAsync()
```

## See Also

* class [Task&lt;TResult&gt;](../Task-1.md)
* struct [Releaser](../AsyncLock.Releaser.md)
* class [AsyncLock](../AsyncLock.md)
* namespace [Microsoft.Coyote.Tasks](../AsyncLock.md)
* assembly [Microsoft.Coyote](../../Microsoft.Coyote.md)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
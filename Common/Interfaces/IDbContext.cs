using System.Collections.Generic;
using Common.Models;

namespace Common.Interfaces
{
    public interface IDbContext
    {
        string GetDbContextType();
        List<TodoItem> Get();
        TodoItem Get(string id);
        TodoItem Create(TodoItem item);
        void Update(string id, TodoItem itemIn);
        void Remove(TodoItem itemIn);
        void Remove(string id);
    }
}

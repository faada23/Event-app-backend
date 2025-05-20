using System.Linq.Expressions;

public interface IRepository<T> where T : class 
{
    void Insert(T entity);
    void Delete(T entity);
    void Update(T entity);
    Task<T?> GetFirstOrDefault(
        Expression<Func<T, bool>> filter, 
        string? includeProperties = null);
    Task<PagedList<T>> GetAll(
        Expression<Func<T, bool>>? filter = null, 
        PaginationParameters? pagParams = null, 
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, 
        string? includeProperties = null);

    Task<int> SaveChangesAsync();
}
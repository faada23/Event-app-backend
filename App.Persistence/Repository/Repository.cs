using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly DatabaseContext _db;
    private readonly DbSet<T> _dbSet;

    public Repository(DatabaseContext db){
        _db = db;
        _dbSet = _db.Set<T>();
    }

    public void Insert(T entity)
    {
        _dbSet.Add(entity);
    }

    public void Delete(T entity)
    {
        if (_db.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
    }


    public async Task<Result<PagedList<T>>> GetAll(
        Expression<Func<T, bool>>? filter = null,
        PaginationParameters? pagParams = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        string? includeProperties = null)
    {
        try
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            query = IncludeProperties(query, includeProperties);

            // происходит на уровне БД
            orderBy ??= q => q.OrderByDescending(e => EF.Property<Guid>(e, "Id"));
            query = orderBy(query);

            var totalItems = await query.CountAsync();

            if (pagParams != null)
            {
                query = query
                    .Skip((pagParams.Page - 1) * pagParams.PageSize)
                    .Take(pagParams.PageSize);
            }

            var items = await query.AsNoTracking().ToListAsync();
            int pageNumber = pagParams?.Page ?? 1;
            int pageSize = pagParams?.PageSize ?? totalItems;

            var pagedList = PagedList<T>.ToPagedList(items, pageNumber, pageSize, totalItems);
            return Result<PagedList<T>>.Success(pagedList);
        }
        catch (Exception ex)
        {
            return Result<PagedList<T>>.Failure(ex.Message, ErrorType.DatabaseError);
        }
    }

    public async Task<Result<T?>> GetFirstOrDefault(Expression<Func<T, bool>> filter, string? includeProperties = null)
    {
       try
        {
            IQueryable<T> query = _dbSet.Where(filter);
            query = IncludeProperties(query, includeProperties);

            var entity = await query.FirstOrDefaultAsync();
            return Result<T?>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<T?>.Failure(ex.Message, ErrorType.DatabaseError);
        }
    }


    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }
    
    private IQueryable<T> IncludeProperties(IQueryable<T> query, string? includeProperties)
    {      
        if (!string.IsNullOrEmpty(includeProperties))
        {
            foreach (var includeProp in includeProperties.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProp);
            }
        }

        return query;
    }

    public async Task<Result<int>> SaveChangesAsync()
    {
        try{
            int changes = await _db.SaveChangesAsync();
            return Result<int>.Success(changes);
        }
        catch(Exception ex){
            return Result<int>.Failure(ex.Message,ErrorType.DatabaseError);
        }
    }
}
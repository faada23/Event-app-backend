public class PagedList<T> : List<T>
{	
    public int CurrentPage { get; private set; }
	public int TotalPages { get; private set; }
	public int PageSize { get; private set; }
	public int TotalItems { get; private set; }

	public PagedList(List<T> items, int totalItems, int pageNumber, int pageSize)
	{	
		AddRange(items);

		TotalItems = totalItems;
		PageSize = pageSize;
		CurrentPage = pageNumber;
		TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
	}

	public static PagedList<T> ToPagedList(List<T> items, int pageNumber, int pageSize, int totalItems)
	{
		return new PagedList<T>(items, totalItems, pageNumber, pageSize);
	}

}
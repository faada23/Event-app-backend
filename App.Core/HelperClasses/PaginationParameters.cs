public class PaginationParameters 
{
	const int MaxPageSize = 100;
	public int Page { get; set; } = 1;
	private int _pageSize = 50;
	public int PageSize
	{
        get
		{
			return _pageSize;
		}
		set
		{
			_pageSize = (value > MaxPageSize) ? MaxPageSize : (value <= 0) ? 10 : value;
		}
	}
}
public interface IDefaultMapper
    {
        TDestination Map<TDestination>(object source);
        TDestination Map<TSource, TDestination>(TSource source);
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
        List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> sourceList);
    }

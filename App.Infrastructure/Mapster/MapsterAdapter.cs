using MapsterMapper;

public class MapsterAdapter : IDefaultMapper
    {
        private readonly IMapper _mapsterMapper;

        public MapsterAdapter(IMapper mapsterMapper)
        {
            _mapsterMapper = mapsterMapper;
        }

        public TDestination Map<TDestination>(object source)
        {
            return _mapsterMapper.Map<TDestination>(source);
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            return _mapsterMapper.Map<TSource, TDestination>(source);
        }

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            return _mapsterMapper.Map(source, destination);
        }

        public List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> sourceList)
        {
            if (sourceList == null) return new List<TDestination>();
            return _mapsterMapper.Map<IEnumerable<TSource>, List<TDestination>>(sourceList);
        }
    }
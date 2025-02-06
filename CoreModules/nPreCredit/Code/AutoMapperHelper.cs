using AutoMapper;
using System;

namespace nPreCredit.Code
{
    public static class AutoMapperHelper
    {
        private static MapperConfiguration mapperConfiguration;
        private static IMapper mapper;

        public static TDestination Map<TSource, TDestination>(TSource source)
        {
            return mapper.Map<TSource, TDestination>(source);
        }

        public static void Initialize(Action<IMapperConfigurationExpression> config)
        {
            mapperConfiguration = new MapperConfiguration(config);
            mapper = mapperConfiguration.CreateMapper();
        }
    }
}
namespace Colosoft.DataServices.Test
{
    public class DataSourceTest
    {
        private readonly IEnumerable<CustomModel> Items = new[]
        {
            new CustomModel
            {
                Id = 1,
                Name = "Test1",
            },
            new CustomModel
            {
                Id = 2,
                Name = "Test2",
            },
        };

        private Task<IEnumerable<CustomModel>> GetItems(DataSourceQueryOptions options, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.Items);
        }

        private Task<IEnumerable<CustomModel>> SearchItems(FilterModel filter, DataSourceQueryOptions options, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.Items.Where(f => f.Name == filter.Name));
        }

        [Fact]
        public async Task GivenGetItemsMethodWhenRefreshDataSourceThenDataSourceFilled()
        {
            var dataSource = new DataSourceQueryHandler<CustomModel>(this.GetItems).ToDataSource(10);
            await dataSource.Refresh(default);

            Assert.NotEmpty(dataSource);
        }

        [Fact]
        public async Task GivenGetItemsMethodWhenRefreshDataSourceFilterableThenDataSourceFilled()
        {
            var dataSource = new DataSourceQueryHandler<FilterModel, CustomModel>(this.SearchItems).ToDataSource(10);
            dataSource.Filter = new FilterModel
            {
                Name = "Test1",
            };
            await dataSource.Refresh(default);

            Assert.NotEmpty(dataSource);
        }
    }
}
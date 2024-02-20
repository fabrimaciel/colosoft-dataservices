using System.Collections.ObjectModel;

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
            new CustomModel
            {
                Id = 3,
                Name = "Test3",
            },
            new CustomModel
            {
                Id = 4,
                Name = "Test4",
            },
            new CustomModel
            {
                Id = 5,
                Name = "Test5",
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

        private async Task<IPagedResult<CustomModel>> CreatePagedResult(
            int page,
            int pageSize,
            Action? getter = null)
        {
            var factory = new PagedResultFactory();

            return await factory.Create(
                (options, cancellationToken) =>
                {
                    getter?.Invoke();
                    return Task.FromResult<IPagedResultContent<CustomModel>>(new PagedResultContent<CustomModel>(
                        this.Items.Skip((options.Page - 1) * options.PageSize).Take(options.PageSize),
                        this.Items.Count()));
                },
                page,
                pageSize,
                default);
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

        [Fact]
        public void GivenItemsWhenRefreshDataSource()
        {
            var observableCollection = new ObservableCollection<CustomModel>(this.Items);

            var dataSource = observableCollection.ToDataSource();

            var changed = false;
            dataSource.CollectionChanged += (_, e) =>
            {
                changed = true;
            };

            observableCollection.Add(new CustomModel
            {
                Id = 6,
                Name = "Test6",
            });

            Assert.True(changed);
        }

        [Fact]
        public async Task RefreshDataSource()
        {
            var callCounter = 0;
            var pagedResult = await this.CreatePagedResult(1, 3, () => callCounter++);

            var dataSource = new DataSource<CustomModel>(pagedResult);

            await dataSource.Refresh(default);

            Assert.Equal(2, callCounter);

            await dataSource.Refresh(default);

            Assert.Equal(3, callCounter);
        }

        [Fact]
        public async Task GivenDataSourceWhenMoveNextPage()
        {
            var pagedResult = await this.CreatePagedResult(1, 3);
            var dataSource = new DataSource<CustomModel>(pagedResult);

            var pageItems1 = dataSource.ToArray();

            await dataSource.MoveNextPage(default);

            var pageItems2 = dataSource.ToArray();

            Assert.NotEqual(pageItems1, pageItems2, new CustomModelEqualityComparer());
        }
    }
}
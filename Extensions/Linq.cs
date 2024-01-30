using System.Linq.Expressions;
using System.Reflection;
using fuquizlearn_api.Enum;
using fuquizlearn_api.Models.Request;
using fuquizlearn_api.Models.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace fuquizlearn_api.Extensions;

public static class Linq
{
    #region Pagination

    public static async Task<PagedResponse<T>> ToPagedAsync<T>(this IQueryable<T> src, PagedRequest payload,
        Expression<Func<T, bool>> where)
        where T : class
    {
        var queryExpression = src.Expression;
        queryExpression = queryExpression.OrderBy(payload.SortBy, payload.SortDirection);

        if (queryExpression.CanReduce)
            queryExpression = queryExpression.Reduce();


        src = src.Provider.CreateQuery<T>(queryExpression);

        if (payload.SortDirection == SortDirectionEnum.Desc) src.OrderDescending();
        if (!payload.Search.IsNullOrEmpty()) src = src.Where(where);

        var total = await src.CountAsync();
        var hasMore = total > payload.Skip + payload.Take;


        var results = new PagedResponse<T>
        {
            Data = await src.Skip(payload.Skip).Take(payload.Take).ToListAsync(),
            Metadata = new PagedMetadata(payload.Skip, payload.Take, total, hasMore)
        };


        return results;
    }

    private static Expression OrderBy(this Expression source, string orderBy, SortDirectionEnum dir)
    {
        if (!string.IsNullOrWhiteSpace(orderBy))
        {
            var orderBys = orderBy.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < orderBys.Length; i++) source = AddOrderBy(source, orderBys[i], dir, i);
        }

        return source;
    }

    private static Expression AddOrderBy(Expression source, string orderBy, SortDirectionEnum dir, int index)
    {
        var orderByMethodName = index == 0 ? "OrderBy" : "ThenBy";
        orderByMethodName += dir == SortDirectionEnum.Desc ? "Descending" : "";

        var sourceType = source.Type.GetGenericArguments().First();
        var parameterExpression = Expression.Parameter(sourceType, "p");
        var orderByExpression = BuildPropertyPathExpression(parameterExpression, orderBy);
        var orderByFuncType = typeof(Func<,>).MakeGenericType(sourceType, orderByExpression.Type);
        var orderByLambda = Expression.Lambda(orderByFuncType, orderByExpression, parameterExpression);

        source = Expression.Call(typeof(Queryable), orderByMethodName,
            new[] { sourceType, orderByExpression.Type }, source, orderByLambda);
        return source;
    }

    private static Expression BuildPropertyPathExpression(this Expression rootExpression, string propertyPath)
    {
        var parts = propertyPath.Split(new[] { '.' }, 2);
        var currentProperty = parts[0];
        var propertyDescription = rootExpression.Type.GetProperty(currentProperty,
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        if (propertyDescription == null)
            throw new KeyNotFoundException(
                $"Cannot find property {rootExpression.Type.Name}.{currentProperty}. The root expression is {rootExpression} and the full path would be {propertyPath}.");

        var propExpr = Expression.Property(rootExpression, propertyDescription);
        if (parts.Length > 1)
            return BuildPropertyPathExpression(propExpr, parts[1]);

        return propExpr;
    }

    #endregion

    #region Order by property name

    public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> source, string propertyName)
    {
        return source.OrderBy(ToLambda<T>(propertyName));
    }

    public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> source, string propertyName)
    {
        return source.OrderByDescending(ToLambda<T>(propertyName));
    }

    private static Expression<Func<T, object>> ToLambda<T>(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(T));
        var property = Expression.Property(parameter, propertyName);
        var propAsObject = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<T, object>>(propAsObject, parameter);
    }

    #endregion
}
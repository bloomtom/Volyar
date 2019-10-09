using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Volyar
{
    public static class LinqExtensions
    {
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, IEnumerable<string> orderByProperties,
                  bool desc)
        {
            if (orderByProperties == null || orderByProperties.Count() == 0)
            {
                return source;
            }

            string command = desc ? "OrderByDescending" : "OrderBy";
            bool firstRun = true;
            foreach (var propertyName in orderByProperties)
            {
                var type = typeof(T);
                var property = type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                var parameter = Expression.Parameter(type, "p");
                var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                var orderByExpression = Expression.Lambda(propertyAccess, parameter);
                var resultExpression = Expression.Call(typeof(Queryable), command, new Type[] { type, property.PropertyType },
                                              source.Expression, Expression.Quote(orderByExpression));
                source = source.Provider.CreateQuery<T>(resultExpression);

                if (firstRun)
                {
                    command = desc ? "ThenByDescending" : "ThenBy";
                    firstRun = false;
                }
            }

            return source;
        }
    }
}

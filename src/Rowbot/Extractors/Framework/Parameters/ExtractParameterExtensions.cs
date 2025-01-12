using System.Linq.Expressions;
using Rowbot.Common;

namespace Rowbot;

public static class ExtractParameterExtensions
{
    public static T GetValue<T>(this ExtractParameter[] parameters, string name)
    {
        var parameter = parameters.FirstOrDefault(x => string.Equals(x.ParameterName, name));
        if (parameter?.ParameterValue is null)
        {
            return default(T)!;
        }
            
        return (T)parameter.ParameterValue;
    }
    
    public static TProperty GetValue<TEntity, TProperty>(this ExtractParameter[] parameters, Expression<Func<TEntity, TProperty>> selector)
    {
        var memberExpression = Ensure.ArgumentIsMemberExpression(selector);
        var propertyInfo = Ensure.MemberExpressionTargetsProperty(memberExpression);
        
        var parameter = parameters.FirstOrDefault(x => string.Equals(x.ParameterName, propertyInfo.Name));
        if (parameter?.ParameterValue is null)
        {
            return default(TProperty)!;
        }
            
        return (TProperty)parameter.ParameterValue;
    }
}
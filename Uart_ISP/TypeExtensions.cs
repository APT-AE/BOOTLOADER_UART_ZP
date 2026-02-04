using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace System
{
    public static class TypeExtensions
    {
        public static Func<object> CreateInstanceDelegate(this Type type)
        {
            NewExpression newExp = Expression.New(type);
            Expression<Func<object>> lambdaExp =
                Expression.Lambda<Func<object>>(newExp, null);
            Func<object> func = lambdaExp.Compile();
            return func;
        }

        public static Func<T, object> CreateInstanceDelegate<T>(this Type type)
        {
            Type paramType = typeof(T);
            var construtor = type.GetConstructor(new Type[] { paramType });
            var param = new ParameterExpression[] { Expression.Parameter(paramType, "arg") };

            NewExpression newExp = Expression.New(construtor, param);
            Expression<Func<T, object>> lambdaExp =
                Expression.Lambda<Func<T, object>>(newExp, param);
            Func<T, object> func = lambdaExp.Compile();
            return func;
        }


        public static Func<T1, T2, object> CreateInstanceDelegate<T1, T2>(this　Type type)
        {
            var types = new Type[] { typeof(T1), typeof(T2) };
            var construtor = type.GetConstructor(types);
            int i = 0;
            var param = types.Select(t => Expression.Parameter(t, "arg" + (i++))).ToArray();
            NewExpression newExp = Expression.New(construtor, param);
            Expression<Func<T1, T2, object>> lambdaExp = Expression.Lambda<Func<T1, T2, object>>(newExp, param);
            Func<T1, T2, object> func = lambdaExp.Compile();
            return func;
        }

        //以下方法中的Lambda表达式“Expression<Func<object[], object>> ”已经定义参数是object[], 而构造函数的参数却不能自动转化。当使用以下代码作测试，


        //////以下代码有bug!
        ////public static Func<object[], object> CreateInstanceDelegate(this Type type, params  object[] args)
        ////{
        ////    var construtor = type.GetConstructor(args.Select(c => c.GetType()).ToArray());
        ////    var param = buildParameters(args);

        ////    NewExpression newExp = Expression.New(construtor, param);
        ////    Expression<Func<object[], object>> lambdaExp =
        ////        Expression.Lambda<Func<object[], object>>(newExp, param);
        ////    Func<object[], object> func = lambdaExp.Compile();
        ////    return func;
        ////}

        ////static ParameterExpression[] buildParameters(object[] args)
        ////{
        ////    int i = 0;
        ////    List<ParameterExpression> list = new List<ParameterExpression>();
        ////    foreach (object arg in args)
        ////    {
        ////        list.Add(Expression.Parameter(arg.GetType(), "arg" + (i++)));
        ////    }
        ////    return list.ToArray();
        ////}

    }
}
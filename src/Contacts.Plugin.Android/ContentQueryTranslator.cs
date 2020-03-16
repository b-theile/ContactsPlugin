//
//  Copyright 2011-2014, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Android.Provider;

namespace Plugin.Contacts
{
    [Android.Runtime.Preserve(AllMembers = true)]
    internal class ContentQueryTranslator
      : ExpressionVisitor
    {
        public ContentQueryTranslator(IQueryProvider provider, ITableFinder tableFinder)
        {
            this._provider = provider;
            this._tableFinder = tableFinder;
            Skip = -1;
            Take = -1;
        }

        public Android.Net.Uri Table
        {
            get;
            private set;
        }

        public bool IsAny
        {
            get;
            private set;
        }

        public bool IsCount { get; private set; }

        public Type ReturnType { get; private set; }

        public IEnumerable<ContentResolverColumnMapping> Projections => this._projections;

        public string QueryString => (this._queryBuilder.Length > 0) ? this._queryBuilder.ToString() : null;

        public string[] ClauseParameters => (this._arguments.Count > 0) ? this._arguments.ToArray() : null;

        public string SortString => (this._sortBuilder != null) ? this._sortBuilder.ToString() : null;

        public int Skip { get; private set; }

        public int Take { get; private set; }

        public Expression Translate(Expression expression)
        {
            Expression expr = Visit(expression);

            if (Table == null)
                Table = this._tableFinder.DefaultTable;

            return expr;
        }

        private readonly IQueryProvider _provider;
        private readonly ITableFinder _tableFinder;
        private bool _fallback = false;
        private List<ContentResolverColumnMapping> _projections;
        private StringBuilder _sortBuilder;
        private readonly List<string> _arguments = new List<string>();
        private readonly StringBuilder _queryBuilder = new StringBuilder();

        protected override Expression VisitMethodCall(MethodCallExpression methodCall)
        {
            if (methodCall.Arguments.Count == 0 || !(methodCall.Arguments[0] is ConstantExpression || methodCall.Arguments[0] is MethodCallExpression))
            {
                this._fallback = true;
                return methodCall;
            }

            Expression expression = base.VisitMethodCall(methodCall);

            methodCall = expression as MethodCallExpression;
            if (methodCall == null)
            {
                this._fallback = true;
                return expression;
            }

            if (!this._fallback)
            {
                if (methodCall.Method.Name == "Where")
                    expression = VisitWhere(methodCall);
                else if (methodCall.Method.Name == "Any")
                    expression = VisitAny(methodCall);
                else if (methodCall.Method.Name == "Select")
                    expression = VisitSelect(methodCall);
                else if (methodCall.Method.Name == "SelectMany")
                    expression = VisitSelectMany(methodCall);
                else if (methodCall.Method.Name == "OrderBy" || methodCall.Method.Name == "OrderByDescending")
                    expression = VisitOrder(methodCall);
                else if (methodCall.Method.Name == "Skip")
                    expression = VisitSkip(methodCall);
                else if (methodCall.Method.Name == "Take")
                    expression = VisitTake(methodCall);
                else if (methodCall.Method.Name == "Count")
                    expression = VisitCount(methodCall);
                else if (methodCall.Method.Name == "First" || methodCall.Method.Name == "FirstOrDefault")
                    expression = VisitFirst(methodCall);
                else if (methodCall.Method.Name == "Single" || methodCall.Method.Name == "SingleOrDefault")
                    expression = VisitSingle(methodCall);
            }

            return expression;
        }

        private Expression VisitFirst(MethodCallExpression methodCall)
        {
            if (methodCall.Arguments.Count > 1)
            {
                VisitWhere(methodCall);
                if (this._fallback)
                    return methodCall;
            }

            Take = 1;
            return methodCall;
        }

        private Expression VisitSingle(MethodCallExpression methodCall)
        {
            if (methodCall.Arguments.Count > 1)
            {
                VisitWhere(methodCall);
                if (this._fallback)
                    return methodCall;
            }

            Take = 2;
            return methodCall;
        }

        private Expression VisitCount(MethodCallExpression methodCall)
        {
            if (methodCall.Arguments.Count > 1)
            {
                VisitWhere(methodCall);
                if (this._fallback)
                    return methodCall;
            }

            IsCount = true;
            return methodCall.Arguments[0];
        }

        private Expression VisitTake(MethodCallExpression methodCall)
        {
            ConstantExpression ce = (ConstantExpression)methodCall.Arguments[1];
            Take = (int)ce.Value;

            return methodCall.Arguments[0];
        }

        private Expression VisitSkip(MethodCallExpression methodCall)
        {
            ConstantExpression ce = (ConstantExpression)methodCall.Arguments[1];
            Skip = (int)ce.Value;

            return methodCall.Arguments[0];
        }

        private Expression VisitAny(MethodCallExpression methodCall)
        {
            if (methodCall.Arguments.Count > 1)
            {
                VisitWhere(methodCall);
                if (this._fallback)
                    return methodCall;
            }

            IsAny = true;
            return methodCall.Arguments[0];
        }

        private class WhereEvaluator
          : ExpressionVisitor
        {
            public WhereEvaluator(ITableFinder tableFinder, Android.Net.Uri existingTable)
            {
                this.tableFinder = tableFinder;
                if (existingTable != null)
                    this.table = new TableFindResult(existingTable, null);
            }

            public Android.Net.Uri Table => this.table.Table;

            public string QueryString => this.builder.ToString();

            public List<string> Arguments => this.arguments;

            public bool Fallback { get; private set; }

            public Expression Evaluate(Expression expression)
            {
                expression = Visit(expression);

                if (!Fallback && this.table != null && this.table.MimeType != null)
                {
                    this.builder.Insert(0, String.Format("(({0} = ?) AND ", ContactsContract.DataColumns.Mimetype));
                    this.builder.Append(")");

                    this.arguments.Insert(0, this.table.MimeType);
                }

                return expression;
            }

            private readonly ITableFinder tableFinder;
            private TableFindResult table;
            private StringBuilder builder = new StringBuilder();
            private readonly List<string> arguments = new List<string>();
            private ContentResolverColumnMapping currentMap;



            protected override Expression VisitMemberAccess(MemberExpression memberExpression)
            {
                TableFindResult result = this.tableFinder.Find(memberExpression);
                if (this.table == null)
                    this.table = result;
                else if (Table != result.Table || result.MimeType != this.table.MimeType)
                {
                    Fallback = true;
                    return memberExpression;
                }

                ContentResolverColumnMapping cmap = this.tableFinder.GetColumn(memberExpression.Member);
                if (cmap == null || cmap.Columns == null)
                {
                    Fallback = true;
                    return memberExpression;
                }

                this.currentMap = cmap;

                if (cmap.Columns.Length == 1)
                    this.builder.Append(cmap.Columns[0]);
                else
                    throw new NotSupportedException();

                return base.VisitMemberAccess(memberExpression);
            }

            protected override Expression VisitConstant(ConstantExpression constant)
            {
                if (constant.Value is IQueryable)
                    return constant;

                if (constant.Value == null)
                    this.builder.Append("NULL");
                else
                {
                    object value = constant.Value;
                    if (this.currentMap != null && this.currentMap.ValueToQueryable != null)
                        value = this.currentMap.ValueToQueryable(value);

                    switch (Type.GetTypeCode(value.GetType()))
                    {
                        case TypeCode.Object:
                            Fallback = true;
                            return constant;

                        case TypeCode.Boolean:
                            this.arguments.Add((bool)value ? "1" : "0");
                            this.builder.Append("?");
                            break;

                        default:
                            this.arguments.Add(value.ToString());
                            this.builder.Append("?");
                            break;
                    }
                }

                return base.VisitConstant(constant);
            }

            protected override Expression VisitBinary(BinaryExpression binary)
            {
                string current = this.builder.ToString();
                this.builder = new StringBuilder();

                Visit(binary.Left);
                if (Fallback)
                    return binary;

                string left = this.builder.ToString();
                this.builder = new StringBuilder();

                string joiner;
                switch (binary.NodeType)
                {
                    case ExpressionType.AndAlso:
                        joiner = " AND ";
                        break;

                    case ExpressionType.OrElse:
                        joiner = " OR ";
                        break;

                    case ExpressionType.Equal:
                        joiner = " = ";
                        break;

                    case ExpressionType.GreaterThan:
                        joiner = " > ";
                        break;

                    case ExpressionType.LessThan:
                        joiner = " < ";
                        break;

                    case ExpressionType.NotEqual:
                        joiner = " IS NOT ";
                        break;

                    default:
                        Fallback = true;
                        return binary;
                }

                Visit(binary.Right);
                if (Fallback)
                {
                    if (binary.NodeType == ExpressionType.AndAlso)
                    {
                        Fallback = false;
                        this.builder = new StringBuilder(current);
                        this.builder.Append("(");
                        this.builder.Append(left);
                        this.builder.Append(")");
                        return binary.Right;
                    }
                    else
                        return binary;
                }

                string right = this.builder.ToString();

                this.builder = new StringBuilder(current);
                this.builder.Append("(");
                this.builder.Append(left);
                this.builder.Append(joiner);
                this.builder.Append(right);
                this.builder.Append(")");

                return binary;
            }
        }

        private Expression VisitWhere(MethodCallExpression methodCall)
        {
            Expression expression = ExpressionEvaluator.Evaluate(methodCall);

            var eval = new WhereEvaluator(this._tableFinder, Table);
            expression = eval.Evaluate(expression);

            if (eval.Fallback || eval.Table == null || (Table != null && eval.Table != Table))
            {
                this._fallback = true;
                return methodCall;
            }

            if (Table == null)
                Table = eval.Table;

            this._arguments.AddRange(eval.Arguments);
            if (this._queryBuilder.Length > 0)
                this._queryBuilder.Append(" AND ");

            this._queryBuilder.Append(eval.QueryString);

            return methodCall.Arguments[0];
        }

        private Type GetExpressionArgumentType(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    return ((ConstantExpression)expression).Value.GetType();
            }

            return null;
        }

        private Expression VisitSelect(MethodCallExpression methodCall)
        {
            MemberExpression me = FindMemberExpression(methodCall.Arguments[1]);
            if (!TryGetTable(me))
                return methodCall;

            ContentResolverColumnMapping column = this._tableFinder.GetColumn(me.Member);
            if (column == null || column.Columns == null)
                return methodCall;

            (this._projections ?? (this._projections = new List<ContentResolverColumnMapping>())).Add(column);
            if (column.ReturnType.IsValueType || column.ReturnType == typeof(string))
                ReturnType = column.ReturnType;

            this._fallback = true;

            Type argType = GetExpressionArgumentType(methodCall.Arguments[0]);
            if (ReturnType == null || (argType != null && ReturnType.IsAssignableFrom(argType)))
                return methodCall.Arguments[0];

            return Expression.Constant(Activator.CreateInstance(typeof(Query<>).MakeGenericType(ReturnType), this._provider));
        }

        //		private Expression VisitSelect (MethodCallExpression methodCall)
        //		{
        //			List<MemberExpression> mes = MemberExpressionFinder.Find (methodCall.Arguments[1], this.tableFinder);
        //			if (!TryGetTable (mes))
        //				return methodCall;
        //
        //			Type returnType = null;
        //
        //			List<Tuple<string, Type>> projs = new List<Tuple<string, Type>>();
        //			foreach (MemberExpression me in mes)
        //			{
        //				Tuple<string, Type> column = this.tableFinder.GetColumn (me.Member);
        //				if (column == null)
        //					return methodCall;
        //				
        //				if (returnType == null)
        //					returnType = column.Item2;
        //				if (returnType != column.Item2)
        //					return methodCall;
        //
        //				projs.Add (column);
        //			}
        //
        //			ReturnType = returnType;
        //			this.fallback = true;
        //
        //			(this.projections ?? (this.projections = new List<Tuple<string, Type>>()))
        //				.AddRange (projs);
        //
        //			return methodCall.Arguments[0];
        //		}

        private Expression VisitSelectMany(MethodCallExpression methodCall)
        {
            List<MemberExpression> mes = MemberExpressionFinder.Find(methodCall, this._tableFinder);
            if (mes.Count > 1)
            {
                this._fallback = true;
                return methodCall;
            }

            if (!TryGetTable(mes))
                return methodCall;

            ContentResolverColumnMapping column = this._tableFinder.GetColumn(mes[0].Member);
            if (column == null || column.ReturnType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            {
                this._fallback = true;
                return methodCall;
            }

            ReturnType = column.ReturnType.GetGenericArguments()[0];

            return Expression.Constant(Activator.CreateInstance(typeof(Query<>).MakeGenericType(ReturnType), this._provider));
            //return methodCall.Arguments[0];
        }

        private Expression VisitOrder(MethodCallExpression methodCall)
        {
            MemberExpression me = FindMemberExpression(methodCall.Arguments[1]);
            if (!TryGetTable(me))
                return methodCall;

            ContentResolverColumnMapping column = this._tableFinder.GetColumn(me.Member);
            if (column != null && column.Columns != null)
            {
                StringBuilder builder = this._sortBuilder ?? (this._sortBuilder = new StringBuilder());
                if (builder.Length > 0)
                    builder.Append(", ");

                if (column.Columns.Length > 1)
                    throw new NotSupportedException();

                builder.Append(column.Columns[0]);

                if (methodCall.Method.Name == "OrderByDescending")
                    builder.Append(" DESC");

                return methodCall.Arguments[0];
            }

            return methodCall;
        }

        private bool TryGetTable(List<MemberExpression> memberExpressions)
        {
            if (memberExpressions.Count == 0)
            {
                this._fallback = true;
                return false;
            }

            Android.Net.Uri existingTable = Table;

            TableFindResult presult = null;

            foreach (MemberExpression me in memberExpressions)
            {
                TableFindResult result = this._tableFinder.Find(me);
                if (result.Table == null)
                {
                    this._fallback = true;
                    return false;
                }

                if (existingTable == null)
                {
                    existingTable = result.Table;
                    presult = result;
                }
                else if (existingTable != result.Table)
                {
                    this._fallback = true;
                    return false;
                }
            }

            if (presult == null)
            {
                this._fallback = true;
                return false;
            }

            Table = presult.Table;

            if (presult.MimeType != null)
            {
                if (this._queryBuilder.Length > 0)
                    this._queryBuilder.Append(" AND ");

                this._queryBuilder.Append(String.Format("({0} = ?)", ContactsContract.DataColumns.Mimetype));
            }
            this._arguments.Add(presult.MimeType);

            return true;
        }

        private bool TryGetTable(MemberExpression me)
        {
            if (me == null)
            {
                this._fallback = true;
                return false;
            }

            TableFindResult result = this._tableFinder.Find(me);
            if (result.MimeType != null)
            {
                if (_queryBuilder.Length > 0)
                    this._queryBuilder.Append(" AND ");

                this._queryBuilder.Append(String.Format("({0} = ?)", ContactsContract.DataColumns.Mimetype));
            }

            this._arguments.Add(result.MimeType);

            if (Table == null)
                Table = result.Table;
            else if (Table != result.Table)
            {
                this._fallback = true;
                return false;
            }

            return true;
        }

        private MemberExpression FindMemberExpression(Expression expression)
        {
            if (expression is UnaryExpression ue)
                expression = ue.Operand;

            if (expression is LambdaExpression le)
                expression = le.Body;

            if (expression is MemberExpression me && this._tableFinder.IsSupportedType(me.Member.DeclaringType))
                return me;

            if (expression is BinaryExpression be)
            {
                me = be.Left as MemberExpression;
                if (me != null && this._tableFinder.IsSupportedType(me.Member.DeclaringType))
                    return me;

                me = be.Right as MemberExpression;
                if (me != null && this._tableFinder.IsSupportedType(me.Member.DeclaringType))
                    return me;
            }

            return null;
        }
    }
}
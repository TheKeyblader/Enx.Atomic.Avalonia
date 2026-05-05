using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Enx.Atomic.Avalonia.Compact;

public abstract record SelectorExpression
{
    public abstract Expression ToExpression(ParameterExpression parameter);
}

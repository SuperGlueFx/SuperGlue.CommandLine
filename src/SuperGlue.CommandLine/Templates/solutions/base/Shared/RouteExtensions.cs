using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SuperGlue
{
    internal static class RouteExtensions
    {
        public static class RouteConstants
        {
            public const string Parameters = "route.Parameters";
            public const string RoutedTo = "route.RoutedTo";
            public const string InputTypes = "route.InputTypes";
            public const string ReverseRoute = "superglue.ReverseRoute";
            public const string CreateRouteFunc = "superglue.CreateRouteFunc";
            public const string EndpointFromInput = "superglue.EndpointFromInput";
            public const string InputsForEndpoint = "superglue.InputsForEndpoint";
        }

        public static RoutingData GetRouteInformation(this IDictionary<string, object> environment)
        {
            return new RoutingData(new ReadOnlyDictionary<string, object>(environment.Get<IDictionary<string, object>>(RouteConstants.Parameters, new Dictionary<string, object>())), 
                environment.Get<object>(RouteConstants.RoutedTo),
                environment.Get<IEnumerable<Type>>(RouteConstants.InputTypes, new List<Type>()));
        }

        public static string RouteTo(this IDictionary<string, object> environment, object input)
        {
            var reverseRoute = environment.Get<Func<object, string>>(RouteConstants.ReverseRoute);

            return reverseRoute == null ? "" : reverseRoute(input);
        }

        public static object GetEndpointForInput(this IDictionary<string, object> environment, object input)
        {
            return environment.Get<Func<object, object>>(RouteConstants.EndpointFromInput, x => null)(input);
        }

        public static IEnumerable<Type> GetInputsForEndpoint(this IDictionary<string, object> environment, object endpoint)
        {
            return environment.Get<Func<object, IEnumerable<Type>>>(RouteConstants.InputsForEndpoint)(endpoint);
        }

        public static void SetRouteDestination(this IDictionary<string, object> environment, object destination, IEnumerable<Type> inputTypes, IDictionary<string, object> parameters = null)
        {
            environment[RouteConstants.RoutedTo] = destination;
            environment[RouteConstants.InputTypes] = inputTypes;
            environment[RouteConstants.Parameters] = parameters ?? new Dictionary<string, object>();
        }

        public static void CreateRoute(this IDictionary<string, object> environment, string pattern, object routeTo, Dictionary<Type, Func<object, IDictionary<string, object>>> inputParameters, params string[] methods)
        {
            environment.Get<Action<string, object, IDictionary<Type, Func<object, IDictionary<string, object>>>, string[]>>(RouteConstants.CreateRouteFunc)(pattern, routeTo, inputParameters, methods);
        }

        public class RoutingData
        {
            public RoutingData(IReadOnlyDictionary<string, object> parameters, object routedTo, IEnumerable<Type> inputTypes)
            {
                Parameters = parameters;
                RoutedTo = routedTo;
                InputTypes = inputTypes;
            }

            public IReadOnlyDictionary<string, object> Parameters { get; private set; }
            public object RoutedTo { get; private set; }
            public IEnumerable<Type> InputTypes { get; private set; }
        }
    }
}
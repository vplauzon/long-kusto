
namespace FlowPlanning.Parsing
{
    internal record PropertyAssignationScript(string Name, PropertyValueScript Value)
    {
        public static long? GetLongProperty(
            IEnumerable<PropertyAssignationScript> properties,
            string name)
        {
            var property = properties
                .Where(p => p.Name == name)
                .FirstOrDefault();

            if (property != null)
            {
                if (property.Value.Integer == null)
                {
                    throw new PlanningException($"'{name}' property should be of type integer");
                }

                return property.Value.Integer;
            }
            else
            {
                return null;
            }
        }
    }
}
using SmartX.Core.Models;

namespace SmartX.Core.Services;

public class DeploymentValidator
{
    public bool ValidateNode(DeploymentNode node)
    {
        if (node == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(node.Name))
        {
            return false;
        }

        if (!node.IsConfigured)
        {
            return false;
        }

        // Base case:
        // A configured node with no children is valid.
        if (node.Children.Count == 0)
        {
            return true;
        }

        foreach (DeploymentNode child in node.Children)
        {
            if (!ValidateNode(child))
            {
                return false;
            }
        }

        return true;
    }
}
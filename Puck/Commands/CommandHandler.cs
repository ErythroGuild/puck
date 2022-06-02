using NodeHandler = System.Func<DSharpPlus.Entities.DiscordInteraction, System.Collections.Generic.Dictionary<string, object>, System.Threading.Tasks.Task>;

namespace Puck.Commands;

abstract class CommandHandler {
	public class CommandTree {
		// The root node can have both group nodes and leaf nodes
		public record class GroupArgs(
				string Name,
				string Description,
				Permissions? DefaultPermissions
			);
		public record class LeafArgs(
			string Name,
			string Description,
			List<CommandOption> Options,
			Permissions? DefaultPermissions
		);

		public record class RootNode {
			public readonly Command Command;
			public readonly List<GroupNode>? Groups;
			public readonly List<LeafNode>? Leaves;
			public readonly NodeHandler? Handler;
			public bool IsLeaf => Handler is not null;

			// Construct a root node that has subcommands.
			public RootNode(GroupArgs args, List<GroupNode> groups, List<LeafNode> leaves) {
				Groups = groups;
				Leaves = leaves;
				Handler = null;

				// Collate subcommands from child nodes.
				List<CommandOption> command_list = new ();
				foreach (GroupNode group in groups)
					command_list.Add(group.Group);
				foreach (LeafNode leaf in leaves)
					command_list.Add(leaf.Command);

				Command = new (
					args.Name,
					args.Description,
					command_list,
					type: ApplicationCommandType.SlashCommand,
					defaultMemberPermissions: args.DefaultPermissions
				);
			}
			// Construct a root node that only has a single command.
			public RootNode(LeafArgs args, NodeHandler handler) {
				Groups = null;
				Leaves = null;
				Handler = handler;
				Command = new (
					args.Name,
					args.Description,
					args.Options,
					type: ApplicationCommandType.SlashCommand,
					defaultMemberPermissions: args.DefaultPermissions
				);
			}
		}
		public record class GroupNode {
			public readonly CommandOption Group;
			public readonly List<LeafNode> Leaves;

			public GroupNode(GroupArgs args, List<LeafNode> leaves) {
				Leaves = leaves;

				// Collate subcommands from child nodes.
				List<CommandOption> command_list = new ();
				foreach (LeafNode leaf in leaves)
					command_list.Add(leaf.Command);

				Group = new (
					args.Name,
					args.Description,
					ApplicationCommandOptionType.SubCommandGroup,
					options: command_list
				);
			}
		}
		public record class LeafNode {
			public readonly CommandOption Command;
			public readonly NodeHandler Handler;

			public LeafNode(CommandOption command, NodeHandler handler) {
				Command = command;
				Handler = handler;
			}
		}

		public RootNode Root;
		public Command Command => Root.Command;

		public CommandTree(RootNode root) {
			Root = root;
		}
	}

	public abstract CommandTree Tree { get; init; }
	public Command Command => Tree.Command;

	private CommandTree.RootNode Root => Tree.Root;
	public Task HandleAsync(DiscordInteraction interaction) {
		List<InteractionArg> arg_list = interaction.GetArgs();
		Dictionary<string, object> args = new ();

		if (Root.IsLeaf) {
			foreach (InteractionArg arg in arg_list)
				args.Add(arg.Name, arg.Value);
			return Root.Handler!.Invoke(interaction, args);
		}

		string subcommand = arg_list[0].Name;

		throw new NotImplementedException();
	}
}

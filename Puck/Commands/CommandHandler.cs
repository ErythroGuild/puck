using static Puck.Commands.CommandHandler.CommandTree;

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
			public readonly List<LeafNode>? Leaves;
			public readonly List<GroupNode>? Groups;
			public readonly NodeHandler? Handler;
			public bool IsLeaf => Handler is not null;

			public readonly IReadOnlyDictionary<string, NodeHandler>? LeafTable;
			public readonly IReadOnlyDictionary<string, GroupNode>? GroupTable;

			// Construct a root node that has subcommands.
			public RootNode(GroupArgs args, List<LeafNode> leaves, List<GroupNode> groups) {
				Leaves = leaves;
				Groups = groups;
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

				// Collate subcommand handlers.
				Dictionary<string, NodeHandler> leafTable = new ();
				foreach (LeafNode leaf in leaves)
					leafTable.Add(leaf.Command.Name, leaf.Handler);
				LeafTable = leafTable;

				Dictionary<string, GroupNode> groupTable = new ();
				foreach (GroupNode group in groups)
					groupTable.Add(group.Group.Name, group);
				GroupTable = groupTable;
			}
			// Construct a root node that only has a single command.
			public RootNode(LeafArgs args, NodeHandler handler) {
				Leaves = null;
				Groups = null;
				Handler = handler;
				Command = new (
					args.Name,
					args.Description,
					args.Options,
					type: ApplicationCommandType.SlashCommand,
					defaultMemberPermissions: args.DefaultPermissions
				);

				LeafTable = null;
				GroupTable = null;
			}
		}
		public record class GroupNode {
			public readonly CommandOption Group;
			public readonly List<LeafNode> Leaves;

			public IReadOnlyDictionary<string, NodeHandler> LeafTable;

			public GroupNode(string name, string description, List<LeafNode> leaves) {
				Leaves = leaves;

				// Collate subcommands from child nodes.
				List<CommandOption> command_list = new ();
				foreach (LeafNode leaf in leaves)
					command_list.Add(leaf.Command);

				Group = new (
					name,
					description,
					ApplicationCommandOptionType.SubCommandGroup,
					options: command_list
				);

				// Collate subcommand handlers.
				Dictionary<string, NodeHandler> leafTable = new ();
				foreach (LeafNode leaf in leaves)
					leafTable.Add(leaf.Command.Name, leaf.Handler);
				LeafTable = leafTable;
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

	private RootNode Root => Tree.Root;
	public Task HandleAsync(DiscordInteraction interaction) {
		List<InteractionArg> arg_list = interaction.GetArgs();
		Dictionary<string, object> args = new ();

		void AddArgs(List<InteractionArg> arg_list) {
			foreach (InteractionArg arg in arg_list)
				args.Add(arg.Name, arg.Value);
		}

		// Invoke from root node.
		if (Root.IsLeaf) {
			AddArgs(arg_list);
			return Root.Handler!
				.Invoke(interaction, args);
		}

		// Invoke leaf child node.
		string subcommand = arg_list[0].Name;
		if (Root.LeafTable!.ContainsKey(subcommand)) {
			arg_list = arg_list[0].GetArgs();
			AddArgs(arg_list);
			return Root.LeafTable![subcommand]
				.Invoke(interaction, args);
		}

		// Invoke from group child node.
		if (Root.GroupTable!.ContainsKey(subcommand)) {
			GroupNode group = Root.GroupTable![subcommand];
			arg_list = arg_list[0].GetArgs();
			subcommand = arg_list[0].Name;

			if (group.LeafTable.ContainsKey(subcommand)) {
				arg_list = arg_list[0].GetArgs();
				AddArgs(arg_list);
				return group.LeafTable[subcommand]
					.Invoke(interaction, args);
			}
		}

		// This should never happen.
		throw new ArgumentException("Unknown slash command.", nameof(interaction));
	}
}

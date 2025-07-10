using System.Collections.Generic;

public class UpgradeNode {
    public int id;
    public UpgradeNode parent;
    public List<UpgradeNode> children;
    public UpgradeStatus status;

    public UpgradeNode(int id, UpgradeNode parent, List<UpgradeNode> children, UpgradeStatus status) {
        this.id = id;
        this.parent = parent;
        this.children = children;
        this.status = status;
    }
}

public class UpgradeTree {
    public UpgradeNode root;
    public UpgradeTreeDataSO treeData;
    public UpgradeTree(UpgradeTreeDataSO data) {
        treeData = data;
    }
}
public enum UpgradeStatus {
    Locked,
    Available,
    Unlocked
}
using System.Collections.Specialized;
using System.Windows;
using Redmine.Net.Api;
using Redmine.Net.Api.Types;
using HtmlAgilityPack;
using System;

namespace Sandox_WPF
{
    public partial class MainWindow : Window
    {
        private string accessToken;
        private NameValueCollection parameters;
        private string prevIssue;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BuildRedmineTree(ref Issue rootIssue, ref TreeNode<string> tree, ref RedmineManager manager)
        {
            if (rootIssue.Children != null)
                foreach (var child in rootIssue.Children)
                {
                    tree.AddChild(child.Id.ToString());
                    try
                    {
                        var childI = manager.GetObject<Issue>(child.Id.ToString(), parameters);
                        var leaf = tree.FindTreeNode(x => x.ToString() == child.Id.ToString());
                        if (childI.Children != null)
                            BuildRedmineTree(ref childI, ref leaf, ref manager);
                    }
                    catch (Redmine.Net.Api.Exceptions.ForbiddenException exp) { }
                }
        }

        private void BuildDashboardTree(ref HtmlNodeCollection rootUl, ref TreeNode<string> tree)
        {
            foreach (var childUl in rootUl)
            {
                if (childUl.Name.Contains("ul"))
                {
                    var liCollection = childUl.ChildNodes;
                    foreach (var liNode in liCollection)
                    {
                        if (liNode.Name == "li")
                        {
                            var issueNum = GetIssueNumber(liNode.InnerHtml.ToString());
                            tree.AddChild(issueNum);
                            try
                            {
                                var a = liNode.ChildNodes;
                                var leaf = tree.FindTreeNode(x => x.ToString() == issueNum.ToString());
                                if (liNode.ChildNodes.Count != 0)
                                    BuildDashboardTree(ref a, ref leaf);
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        private string GetIssueNumber(string rawStr)
        {
            return rawStr.Substring(rawStr.IndexOf("/issues") + 8, 5);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (!ParentCB.IsChecked.Value && !ExistCB.IsChecked.Value)
                MessageBox.Show("Выберите что-то хотите проанализировать!");
            else {                
                accessToken = accessTokenRM.Text;
                parameters = new NameValueCollection { { RedmineKeys.INCLUDE, RedmineKeys.CHILDREN } };
                try
                {
                    RedmineManager manager = new RedmineManager("https://redmine.minsvyazdnr.ru", accessToken);
                    if (manager.GetObject<Issue>(redmineIssue.Text, parameters) != null)
                    {
                        Issue issue = manager.GetObject<Issue>(redmineIssue.Text, parameters);
                        string description = issue.Description;
                        HtmlDocument parsedDescription = new HtmlDocument();
                        parsedDescription.LoadHtml(description);
                        HtmlNodeCollection childs = parsedDescription.DocumentNode.ChildNodes;
                        TreeNode<string> treeRM = new TreeNode<string>("root");
                        BuildRedmineTree(ref issue, ref treeRM, ref manager);
                        TreeNode<string> treeDashboard = new TreeNode<string>("root");
                        BuildDashboardTree(ref childs, ref treeDashboard);
                        IssueList.Items.Clear();
                        CompareTrees(treeRM, treeDashboard);
                    }
                    else
                        MessageBox.Show("Задача не найдена!");
                }
                catch (Exception exp)
                {
                    MessageBox.Show("Возникла непредвиденная ошибка!\n" + exp);
                }
            }
        }

        private void CompareTrees(TreeNode<string> tableTree, TreeNode<string> dashboardTree)
        {
            foreach (var node in tableTree)
            {
                var compareTree = dashboardTree.FindTreeNode(x => x.Data == node.Data);
                if (compareTree != null)
                {
                    if (node.Parent != null && node.Parent.ToString() != null && node.ToString() != "root")
                    {
                        /*if (compareTree.Level != node.Level)
                            IssueList.Items.Add("Задача №" + node.Data + " не на своем уровне иерархии, верный " + node.Level + ", а показан " + compareTree.Level);*/
                        if (compareTree.Parent.ToString() != node.Parent.ToString() && ParentCB.IsChecked.Value)
                            IssueList.Items.Add("Задача №" + node.Data + " отображена с неверным родителем, верный №" + node.Parent.Data + ", а показан №" + compareTree.Parent.Data);
                    }
                }
                else if (ExistCB.IsChecked.Value)
                    IssueList.Items.Add("Задача №" + node.Data + " не найдена");
            }
        }
    }
}
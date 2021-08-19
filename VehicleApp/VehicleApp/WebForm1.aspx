<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm1.aspx.cs" Inherits="VehicleApp.WebForm1"  %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link href="StyleSheet1.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <h2>Add Vehicle</h2>
            <asp:Label ID="Label3" runat="server" Text="Year:" CssClass="orange" ></asp:Label>
            <asp:TextBox ID="YearTextBox" runat="server" CssClass="addMarginTopAndLeft" Width="214px"  ></asp:TextBox>
            <asp:Label ID="YearErrorMessageLabel" runat="server" Text="" CssClass="red"></asp:Label>
            <br />
            <asp:Label ID="Label1" runat="server" Text="Make:" CssClass="orange" ></asp:Label>
            <asp:TextBox ID="MakeTextBox" runat="server" CssClass="addMarginTopAndLeft" Width="201px"></asp:TextBox>
            <asp:Label ID="MakeErrorMessageLabel" runat="server" Text="" CssClass="red"></asp:Label>
            <br />
            <asp:Label ID="Label2" runat="server" Text="Model:" CssClass="orange"></asp:Label>
            <asp:TextBox ID="ModelTextBox" runat="server" CssClass="addMarginTopAndLeft" Width="194px"></asp:TextBox>
            <asp:Label ID="ModelErrorMessageLabel" runat="server" Text="" CssClass="red"></asp:Label>
            <br />
            <asp:Label ID="Label4" runat="server" Text="Impound Date:" CssClass="orange"></asp:Label>
            <asp:TextBox ID="ImpoundTextBox" runat="server" CssClass="addMarginTopAndLeft" Width="133px"></asp:TextBox>
            <asp:Label ID="ImpoundErrorMessageLabel" runat="server" Text="" CssClass="red"></asp:Label>
            <br />
            <asp:Label ID="Label5" runat="server" Text="Please enter the Impound Date in yyyy-mm-dd format." CssClass="whiteText" ></asp:Label>
            <br />
            <asp:Button ID="AddButton" runat="server" OnClick="AddButton_Click" Text="Add" CssClass="addButton" Height="30px" Width="80px" />
        </div>
        <h2>Vehicles</h2>
        <asp:GridView ID="GridView1" runat="server" OnRowDeleting="GridView1_RowDeleting" AutoGenerateColumns="False" CssClass="gridView" >
            <AlternatingRowStyle CssClass="lightBlue" />
            <Columns>
                <asp:ButtonField ButtonType="Button" CommandName="Delete" HeaderText="Delete" ShowHeader="True" Text="Delete" >
                <ControlStyle CssClass="white" />
                </asp:ButtonField> 
                <asp:TemplateField HeaderText="UniqueID">  
                    <ItemTemplate>  
                        <asp:Label ID="lbl_UniqueID" runat="server" Text='<%#Eval("UniqueID") %>'></asp:Label>  
                    </ItemTemplate>  
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Year">  
                    <ItemTemplate>  
                        <asp:Label ID="lbl_Year" runat="server" Text='<%#Eval("Year") %>'></asp:Label>  
                    </ItemTemplate>  
                </asp:TemplateField> 
                <asp:TemplateField HeaderText="Make">  
                    <ItemTemplate>  
                        <asp:Label ID="lbl_Make" runat="server" Text='<%#Eval("Make") %>'></asp:Label>  
                    </ItemTemplate>  
                </asp:TemplateField> 
                <asp:TemplateField HeaderText="Model">  
                    <ItemTemplate>  
                        <asp:Label ID="lbl_Model" runat="server" Text='<%#Eval("Model") %>'></asp:Label>  
                    </ItemTemplate>  
                </asp:TemplateField> 
                <asp:TemplateField HeaderText="ImpoundDate">  
                    <ItemTemplate>  
                        <asp:Label ID="ImpoundDate" runat="server" Text='<%#Eval("ImpoundDate") %>'></asp:Label>  
                    </ItemTemplate>  
                </asp:TemplateField>
                <asp:TemplateField HeaderText="AuctionDate">  
                    <ItemTemplate>  
                        <asp:Label ID="AuctionDate" runat="server" Text='<%#Eval("AuctionDate") %>'></asp:Label>  
                    </ItemTemplate>  
                </asp:TemplateField>
                <asp:TemplateField HeaderText="DateTimeCreated">  
                    <ItemTemplate>  
                        <asp:Label ID="DateTimeCreated" runat="server" Text='<%#Eval("DateTimeCreated") %>'></asp:Label>  
                    </ItemTemplate>  
                </asp:TemplateField>
                <asp:TemplateField HeaderText="IsDeleted">  
                    <ItemTemplate>  
                        <asp:Label ID="IsDeleted" runat="server" Text='<%#Eval("IsDeleted") %>'></asp:Label>  
                    </ItemTemplate>  
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
                No vehicles found.
            </EmptyDataTemplate>
        </asp:GridView>
    </form>
</body>
</html>

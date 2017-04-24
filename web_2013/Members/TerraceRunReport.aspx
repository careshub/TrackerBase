<%@ Page Title="" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="TerraceRunReport.aspx.vb"
  Inherits="TerraceRunReport" EnableEventValidation="false" %>

<%@ MasterType VirtualPath="~/Site.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="Server">
  <asp:Literal ID="paramHolder" runat="server" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="Server">
  <asp:HiddenField ID="uxHiddenProjectAddress" runat="server" />
  <div class="whitespace-double"></div>
  <div id="uxContainer">
    <ul class="spaced">
      <li>
        <label runat="server" id="uxInfo"></label>
      </li>
      <li>
        <asp:Button class="main-menu" ID="uxOpenReport" Text="Open as File" runat="server"
          title="Open errors and report as a text file" />

        Download Fortran run files: <a runat="server" id="uxFortranRunZip" href="No zip file">No zip file</a>
      </li>
      <li>
        <label runat="server" id="uxErrorsInfo"></label>
      </li>
      <li>
        <asp:TextBox ID="uxErrors" runat="server" TextMode="MultiLine" Width="100%" Height="150px">
        </asp:TextBox>
      </li>
      <li>
        <label runat="server" id="uxReportInfo"></label>
      </li>
      <li>
        <asp:TextBox ID="uxReport" runat="server" TextMode="MultiLine" Width="100%" Height="600px">
        </asp:TextBox>
      </li>
    </ul>

  </div>
</asp:Content>

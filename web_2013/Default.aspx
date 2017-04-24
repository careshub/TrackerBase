<%@ Page Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false"
  CodeFile="Default.aspx.vb" Inherits="_Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
  <script type="text/javascript" src="/Scripts/Default.js"></script>
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">

  <div class="colmask threecol whitespace">
    <div class="colmid">
      <div class="colleft">
        <div class="col1">
          <h2>Terrace Location</h2>
          <p>A suite of tools supporting terrace planning.</p>
          <div class="p">
            Current key capabilities of Terrace Tools include:
      <ul class="user-functions">
        <li>
          <label>Password protected accounts for managing multiple projects.</label></li>
        <li>
          <label>Feature delineation including terrace areas, ridgelines, waterways and other farm features.</label></li>
        <li>
          <label>
            Collected data exports to other programs.  
        Exports can include shape files for use in GIS programs and supporting layers provided by the Missouri Clipper.</label></li>
      </ul>
          </div>
          <p>&nbsp;</p>
          <p>
            Please click on the About and News menu tabs for usage and contact information.
          </p>
        </div>
        <div class="col2">
          <img id="uxImageMain" class="main-image" alt="TerLoc: a tool to create and manage terraces" src="/Images/TerraceProject.PNG" />
        </div>
        <div class="col3" style="padding-top: 15px;">
          <ul class="example1">
            <li>
              <input type="submit" id="uxSignUp" value="REGISTER FOR TERLOC" title="Register" class="login-button" onclick="Redirect('/Register'); return false;" /></li>
            <li>
              <input type="submit" id="uxLogin" value="LOG IN TO TERLOC" title="Log in" class="login-button button-2h" onclick="Redirect('/Login'); return false;" /></li>
            <li>
              <asp:Label ID="uxSiteUseWarningDefault" runat="server" CssClass="warning-text">This is a development site for Terrace. Please use <a href="http://terrace.missouri.edu">terrace.missouri.edu</a>.</asp:Label>
            </li>
            <li class="whitespace-double"></li>
          </ul>
        </div>
      </div>
    </div>
  </div>
  <div class="footer text-small">
    Copyright © 2015 — Curators of the <a href="http://www.umsystem.edu" target="_blank">University of Missouri</a>. All rights reserved. 
  <a href="http://www.missouri.edu/dmca/" target="_blank">DMCA</a> and <a href="http://www.missouri.edu/copyright.php" target="_blank">other copyright information</a>.
  </div>
</asp:Content>

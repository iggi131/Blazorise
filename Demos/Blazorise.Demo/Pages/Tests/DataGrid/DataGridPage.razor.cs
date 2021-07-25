﻿#region Using directives
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazorise.DataGrid;
using Blazorise.DataGrid.Utils;
using Blazorise.Demo.Data;
using Blazorise.Demo.Models;
using Microsoft.AspNetCore.Components;
#endregion

namespace Blazorise.Demo.Pages.Tests.DataGrid
{
    public partial class DataGridPage
    {

        #region Members

        Employee editModel = new();

        DataGridEditMode editMode = DataGridEditMode.Form;
        DataGridSortMode sortMode = DataGridSortMode.Multiple;
        DataGridSelectionMode selectionMode = DataGridSelectionMode.Single;
        DataGridCommandMode commandsMode = DataGridCommandMode.Commands;
        TableResizeMode resizableMode = TableResizeMode.Header;

        DataGrid<Employee> dataGrid;
        public int currentPage { get; set; } = 1;

        bool editable = true;
        bool fixedHeader = false;
        bool virtualize = false;
        bool resizable = true;
        bool sortable = true;
        bool filterable = true;
        bool showPager = true;
        bool showPageSizes = true;
        bool largeDataMode = false;
        bool showButtonRow = true;

        Employee selectedEmployee;
        List<Employee> selectedEmployees;

        List<Employee> employeeList;
        int totalEmployees;

        string selectedGenderFilter;
        string selectedCityFilter;

        Random random = new();

        List<Employee> dataModels = new();
        List<Employee> inMemoryDataModels;

        #endregion

        #region Methods
        [Inject] EmployeeData EmployeeData { get; set; }

        protected override async Task OnInitializedAsync()
        {
            inMemoryDataModels = await EmployeeData.GetDataAsync();
            dataModels = inMemoryDataModels.Take( 50 ).ToList();
            totalEmployees = dataModels.Count;
            await base.OnInitializedAsync();
        }

        public void OnVirtualizeChanged( bool toVirtualize )
        {
            virtualize = toVirtualize;
            if ( virtualize )
                dataModels = inMemoryDataModels.ToList();
            else
                dataModels = inMemoryDataModels.Take( 50 ).ToList();
        }

        public void CheckEmail( ValidatorEventArgs validationArgs )
        {
            ValidationRule.IsEmail( validationArgs );

            if ( validationArgs.Status == ValidationStatus.Error )
            {
                validationArgs.ErrorText = "Email has to be a valid Email";
            }
        }

        public void CheckFirstName( ValidatorEventArgs validationArgs )
        {
            ValidationRule.IsNotEmpty( validationArgs );

            if ( validationArgs.Status == ValidationStatus.Error )
            {
                validationArgs.ErrorText = "First name has to be provided";
            }
        }

        void OnEmployeeNewItemDefaultSetter( Employee employee )
        {
            employee.Salary = 100.0M;
            employee.IsActive = true;
        }

        void OnRowInserted( SavedRowItem<Employee, Dictionary<string, object>> e )
        {
            //var employee = e.Item;

            //employee.Id = dataModels?.Max( x => x.Id ) + 1 ?? 1;

            //dataModels.Add( employee );
        }

        void OnRowUpdated( SavedRowItem<Employee, Dictionary<string, object>> e )
        {
            //var employee = e.Item;

            //employee.FirstName = (string)e.Values["FirstName"];
            //employee.LastName = (string)e.Values["LastName"];
            //employee.Email = (string)e.Values["Email"];
            //employee.City = (string)e.Values["City"];
            //employee.Zip = (string)e.Values["Zip"];
            //employee.DateOfBirth = (DateTime?)e.Values["DateOfBirth"];
            //employee.Childrens = (int?)e.Values["Childrens"];
            //employee.Gender = (string)e.Values["Gender"];
            //employee.Salary = (decimal)e.Values["Salary"];
        }

        void OnRowRemoved( Employee model )
        {
            //if ( dataModels.Contains( model ) )
            //{
            //    dataModels.Remove( model );
            //}
        }

        string customFilterValue;

        private Task OnCustomFilterValueChanged( string e )
        {
            customFilterValue = e;
            return dataGrid.Reload();
        }

        bool OnCustomFilter( Employee model )
        {
            if ( string.IsNullOrEmpty( customFilterValue ) )
                return true;

            return
                model.FirstName?.Contains( customFilterValue, StringComparison.OrdinalIgnoreCase ) == true
                || model.LastName?.Contains( customFilterValue, StringComparison.OrdinalIgnoreCase ) == true
                || model.Email?.Contains( customFilterValue, StringComparison.OrdinalIgnoreCase ) == true;
        }

        async Task OnReadData( DataGridReadDataEventArgs<Employee> e )
        {
            if ( !e.CancellationToken.IsCancellationRequested )
            {
                List<Employee> response = null;

                var filteredData = await FilterData( e.Columns );

                // this can be call to anything, in this case we're calling a fictional api
                if ( e.ReadDataMode is DataGridReadDataMode.Virtualize )
                    response = filteredData.Skip( e.VirtualizeOffset ).Take( e.VirtualizeCount ).ToList();
                else if ( e.ReadDataMode is DataGridReadDataMode.Paging )
                    response = filteredData.Skip( ( e.Page - 1 ) * e.PageSize ).Take( e.PageSize ).ToList();
                else
                    throw new Exception( "Unhandled ReadDataMode" );

                await Task.Delay( random.Next( 100 ) );
                if ( !e.CancellationToken.IsCancellationRequested )
                {
                    totalEmployees = filteredData.Count;
                    employeeList = new List<Employee>( response ); // an actual data for the current page
                }
            }
        }


        /// <summary>
        /// Simple demo purpose example filter
        /// </summary>
        /// <param name="dataGridColumns"></param>
        /// <returns></returns>
        public Task<List<Employee>> FilterData( IEnumerable<DataGridColumnInfo> dataGridColumns )
        {
            var filteredData = dataModels.ToList();
            var sortByColumns = dataGridColumns.Where( x => x.SortDirection != SortDirection.None );
            var firstSort = true;
            if ( sortByColumns?.Any() ?? false )
            {
                IOrderedEnumerable<Employee> sortedCols = null;
                foreach ( var sortByColumn in sortByColumns )
                {
                    var valueGetter = FunctionCompiler.CreateValueGetter<Employee>( sortByColumn.Field );

                    if ( firstSort )
                    {
                        if ( sortByColumn.SortDirection == SortDirection.Ascending )
                            sortedCols = dataModels.OrderBy( x => valueGetter( x ) );
                        else
                            sortedCols = dataModels.OrderByDescending( x => valueGetter( x ) );

                        firstSort = false;
                    }
                    else
                    {
                        if ( sortByColumn.SortDirection == SortDirection.Ascending )
                            sortedCols = sortedCols.ThenBy( x => valueGetter( x ) );
                        else
                            sortedCols = sortedCols.ThenByDescending( x => valueGetter( x ) );
                    }
                }
                filteredData = sortedCols.ToList();
            }

            if ( dataGrid.CustomFilter != null )
                filteredData = filteredData.Where( item => item != null && dataGrid.CustomFilter( item ) ).ToList();

            foreach ( var column in dataGridColumns.Where( x => !string.IsNullOrWhiteSpace( x.SearchValue?.ToString() ) ) )
            {

                var valueGetter = FunctionCompiler.CreateValueGetter<Employee>( column.Field );
                //if ( column.CustomFilter != null )
                //{
                //    filteredData = from item in filteredData.Where(x=> column. valueGetter( x))
                //            let cellRealValue = column.GetValue( item )
                //            where column.CustomFilter( cellRealValue, column.Filter.SearchValue )
                //            select item;
                //}
                //else
                //{

                filteredData = filteredData.Where( x => valueGetter( x )?.ToString().IndexOf( column.SearchValue.ToString(), StringComparison.OrdinalIgnoreCase ) >= 0 ).ToList();
                //}

            }
            return Task.FromResult( filteredData );
        }

        Task Reset()
        {
            currentPage = 1;
            return dataGrid.Reload();
        }

        private bool OnGenderCustomFilter( object itemValue, object searchValue )
        {
            if ( searchValue is string genderFilter )
            {
                return genderFilter == "*" || genderFilter == itemValue?.ToString();
            }

            return true;
        }

        private void OnFilteredDataChanged( DataGridFilteredDataEventArgs<Employee> eventArgs )
        {
            Console.WriteLine( $"Filter changed > Items: {eventArgs.FilteredItems}; Total: {eventArgs.TotalItems};" );
        }

        private void OnSortChanged( DataGridSortChangedEventArgs eventArgs )
        {
            Console.WriteLine( $"Sort changed > Field: {eventArgs.FieldName}; Direction: {eventArgs.SortDirection};" );
        }

        private string TitleFromGender( string gender )
        {
            return gender switch
            {
                "M" => "Mr.",
                "F" => "Mrs.",
                _ => string.Empty,
            };
        }

        private string TitleToName( string title, object name )
        {
            if ( string.IsNullOrEmpty( title ) )
                return $"{name}";

            return $"{title} {name}";
        }

        #endregion
    }
}
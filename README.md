# Intelligent Data Management

This project implements a data warehouse consisting of three databases: one primary SQL database (PostgreSQL) and two secondary NoSQL databases (MongoDB and Redis). The application is built using **ASP.NET Core MVC** and Docker for containerization, and it demonstrates querying and interaction between different types of databases for e-commerce data.

## Table of Contents
- [Project Overview](#project-overview)
- [Dataset](#dataset)
- [Technologies Used](#technologies-used)
- [Database Architecture](#database-architecture)
  - [PostgreSQL](#postgresql)
  - [MongoDB](#mongodb)
  - [Redis](#redis)
- [Features](#features)
  - [Data Queries](#data-queries)
  - [Heartbeat Service](#heartbeat-service)
  - [Nightly Synchronization](#nightly-synchronization)
  - [User Management](#user-management)
- [Installation](#installation)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## Project Overview

The goal of this project is to showcase how relational and non-relational databases can be integrated into a single data management system. The system uses **PostgreSQL** for structured data, **MongoDB** for document-based storage, and **Redis** for high-speed key-value operations. The system supports querying data across all three databases and includes synchronization features to keep the databases updated.

## Dataset

The dataset used for this project is an e-commerce dataset that contains information about:
- Invoices
- Products
- Customers
- Countries
- Sales trends

The data is initialized in each database according to the specific requirements of that database's data model. For example, **PostgreSQL** uses a star schema for structured data, while **MongoDB** and **Redis** use more flexible approaches.

## Technologies Used

- **ASP.NET Core MVC**: Backend web framework for building the system's controllers, models, and views.
- **Docker**: Used for containerization of the application and databases.
- **PostgreSQL**: The primary relational database for structured data and complex queries.
- **MongoDB**: Document-based NoSQL database for handling unstructured data.
- **Redis**: Key-value store used for fast, in-memory operations and pre-aggregated data queries.

## Database Architecture

### PostgreSQL

PostgreSQL is used as the primary relational database. It handles structured data with relationships between entities such as **Invoices**, **Products**, **Customers**, and **Countries**. The system leverages SQL for real-time querying and analysis. A **star schema** is used to organize the data.

### MongoDB

MongoDB stores unstructured or semi-structured data, making it suitable for managing documents such as sales and customer information. The project uses MongoDB’s aggregation framework for queries and supports document-based data storage and querying.

### Redis

Redis is used as an in-memory key-value store for caching and real-time querying. It stores pre-aggregated queries such as total sales by country and customer lifetime value. The system is designed to handle fast data retrieval through Redis.

## Features

### Data Queries

The system allows for queries across all three databases, including:
- **Total Sales by Country**
- **Sales by Product**
- **Invoice Summaries**
- **Customer Lifetime Value**
- **Sales Trends**

### Heartbeat Service

A **Heartbeat Service** monitors the availability of the databases. It periodically checks the connections to PostgreSQL, MongoDB, and Redis, and logs the status of each connection.

### Nightly Synchronization

The system features a **Nightly Synchronization** that ensures the data in **MongoDB** and **Redis** is up to date with **PostgreSQL**. This synchronization process runs automatically but can also be triggered manually by an administrator.

### User Management

The application includes user authentication and role-based authorization using the **ASP.NET Identity Framework**. There are two types of users:
- **Administrators**: Can add new data, trigger synchronization, and have full access to the databases.
- **Users**: Can only query data and have limited access.

## Installation

1. Clone the repository:
    ```bash
    git clone https://github.com/OskarEM/intelligent-data-management.git
    cd intelligent_data_management
    ```

2. Install dependencies:
    ```bash
    dotnet restore
    ```

3. Build and run Docker containers:
    ```bash
    docker-compose up --build
    ```

4. The application will be available at `http://localhost:5000`.

## Usage

1. **Query Data**: Access the system via the browser and navigate to the query options for PostgreSQL, MongoDB, or Redis.
2. **Admin Controls**: Log in as an administrator to add data or manually trigger synchronization.
3. **Heartbeat Monitoring**: The system continuously monitors the database connections and reports any issues.


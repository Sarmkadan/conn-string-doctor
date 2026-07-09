# conn-string-doctor

Diagnoses connection strings: parsing, reachability, TLS, pooling, timeouts.

> v0.1 in progress.

## FluentConnectionStringBuilder

The `FluentConnectionStringBuilder` class provides a fluent interface for constructing provider-specific connection strings. 
It allows you to build connection strings for various database providers using method chaining, with proper escaping and provider-specific formatting.

Here's an example usage:

## ConnectionStringInfo

The `ConnectionStringInfo` class represents parsed connection string details, including provider type, server, port, database, user credentials, and additional properties. It is used by conversion and validation components to process connection strings.


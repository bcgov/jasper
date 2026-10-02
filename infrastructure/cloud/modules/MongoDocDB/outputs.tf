

output mongo_cluster_endpoint {
  description = "The connection endpoint for the DocumentDB cluster."
  value       = aws_docdb_cluster.mongo_cluster.endpoint
}

output mongo_cluster_arn {
  description = "The ARN of the DocumentDB cluster."
  value       = aws_docdb_cluster.mongo_cluster.arn
}
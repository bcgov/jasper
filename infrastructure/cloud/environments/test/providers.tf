terraform {
  required_version = "~> 1.14.3"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0.0"
    }

    tls = {
      source  = "hashicorp/tls"
      version = "4.1.0"
    }
  }

  backend "s3" {
  }
}

provider "aws" {
  region = var.region
}

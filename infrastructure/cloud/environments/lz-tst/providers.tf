terraform {
  required_version = "~> 1.12.1"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.98.0"
    }

    tls = {
      source  = "hashicorp/tls"
      version = "4.1.0"
    }
  }

  backend "s3" {
    encrypt      = true  
    use_lockfile = true
  }
}

provider "aws" {
  region = var.region
}

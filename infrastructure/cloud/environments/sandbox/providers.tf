terraform {
  required_version = "~> 1.9.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }

    tls = {
      source  = "hashicorp/tls"
      version = "4.0.5"
    }
  }

  backend "s3" {
    bucket         = "terraform-remote-state-sandbox-12345"
    key            = "terraform.tfstate"
    region         = "ca-central-1"
    dynamodb_table = "terraform-remote-state-lock-12345"
  }

}



provider "aws" {
  region = "ca-central-1"
}
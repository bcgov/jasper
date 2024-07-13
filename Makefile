#!make


export AWS_PROFILE ?= jasperlocal
export AWS_DEFAULT_REGION ?= ca-central-1
export AWS_ACCOUNT ?= 381491824201
export TOOLKIT_STACK_NAME= CDK-Bootstrap-jasper-dev
export QUALIFIER= jasperdev
export BRANCH_NAME= dev
export ENV_NAME= dev



run-bootstrap-jasper:
	@echo "Running bootstrap"
	@cd infrastructure/jasper-aws-bootstrap && cdk bootstrap aws://$(AWS_ACCOUNT)/$(AWS_DEFAULT_REGION) --toolkit-stack-name $TOOLKIT_STACK_NAME --qualifier $QUALIFIER --profile $(AWS_PROFILE)
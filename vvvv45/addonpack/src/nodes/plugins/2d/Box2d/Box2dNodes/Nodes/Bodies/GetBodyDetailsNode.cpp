#include "StdAfx.h"
#include "GetBodyDetailsNode.h"
#include "../../Internals/Data/BodyCustomData.h"

using namespace System::Collections::Generic;

namespace VVVV 
{
	namespace Nodes 
	{

		GetBodyDetailsNode::GetBodyDetailsNode(void)
		{
			this->m_shapes = gcnew ShapeDataType();
		}

		void GetBodyDetailsNode::SetPluginHost(IPluginHost^ Host) 
		{
			this->FHost = Host;

			this->FHost->CreateNodeInput("Bodies",TSliceMode::Dynamic,TPinVisibility::True,this->vInBodies);
			this->vInBodies->SetSubType(ArrayUtils::DoubleGuidArray(BodyDataType::GUID,GroundDataType::GUID),BodyDataType::FriendlyName);

			this->FHost->CreateValueOutput("Position",2,ArrayUtils::Array2D(),TSliceMode::Dynamic,TPinVisibility::True,this->vOutPosition);
			this->vOutPosition->SetSubType2D(Double::MinValue,Double::MaxValue,0.01,0.0,0.0,false,false,false);

			this->FHost->CreateValueOutput("Rotation",1,ArrayUtils::Array1D(),TSliceMode::Dynamic,TPinVisibility::True,this->vOutRotation);
			this->vOutRotation->SetSubType(Double::MinValue,Double::MaxValue,0.01,0.0,false,false,false);

			this->FHost->CreateValueOutput("Velocity",2,ArrayUtils::Array2D(),TSliceMode::Dynamic,TPinVisibility::True,this->vOutVelocity);
			this->vOutVelocity->SetSubType2D(Double::MinValue,Double::MaxValue,0.01,0.0,0.0,false,false,false);

			this->FHost->CreateValueOutput("Is Dynamic",1,ArrayUtils::Array1D(),TSliceMode::Dynamic,TPinVisibility::True,this->vOutIsDynamic);
			this->vOutIsDynamic->SetSubType(Double::MinValue,Double::MaxValue,1,0.0,false,true,false);

			this->FHost->CreateValueOutput("Is Sleeping",1,ArrayUtils::Array1D(),TSliceMode::Dynamic,TPinVisibility::True,this->vOutIsSleeping);
			this->vOutIsSleeping->SetSubType(Double::MinValue,Double::MaxValue,1,0.0,false,true,false);

			this->FHost->CreateValueOutput("Is Frozen",1,ArrayUtils::Array1D(),TSliceMode::Dynamic,TPinVisibility::True,this->vOutIsFrozen);
			this->vOutIsFrozen->SetSubType(Double::MinValue,Double::MaxValue,1,0.0,false,true,false);

			this->FHost->CreateValueOutput("Mass",1,ArrayUtils::Array1D(),TSliceMode::Dynamic,TPinVisibility::True,this->vOutMass);
			this->vOutMass->SetSubType(Double::MinValue,Double::MaxValue,0.01,0.0,false,false,false);

			this->FHost->CreateValueOutput("Inertia",1,ArrayUtils::Array1D(),TSliceMode::Dynamic,TPinVisibility::True,this->vOutInertia);
			this->vOutInertia->SetSubType(Double::MinValue,Double::MaxValue,0.01,0.0,false,false,false);

			this->FHost->CreateStringOutput("Custom",TSliceMode::Dynamic,TPinVisibility::True,this->vOutCustom);
			this->vOutCustom->SetSubType("",false);

			this->FHost->CreateNodeOutput("Shapes",TSliceMode::Dynamic,TPinVisibility::True,this->vOutShapes);
			this->vOutShapes->SetSubType(ArrayUtils::SingleGuidArray(ShapeDataType::GUID),ShapeDataType::FriendlyName);
			this->vOutShapes->SetInterface(this->m_shapes);

			this->FHost->CreateStringOutput("Shape Types",TSliceMode::Dynamic,TPinVisibility::True,this->vOutShapeType);
			this->vOutShapeType->SetSubType("",false);

			this->FHost->CreateValueOutput("Shape Count",1,ArrayUtils::Array1D(),TSliceMode::Dynamic,TPinVisibility::True,this->vOutShapeCount);
			this->vOutShapeCount->SetSubType(0,Double::MaxValue,1,0.0,false,false,true);


			this->FHost->CreateValueOutput("Body Id",1,ArrayUtils::Array1D(),TSliceMode::Dynamic,TPinVisibility::True,this->vOutId);
			this->vOutId->SetSubType(Double::MinValue,Double::MaxValue,1,0.0,false,false,true);

			//this->FHost->CreateValueOutput("Velocity",2,ArrayUtils::Array2D(),TSliceMode::Dynamic,TPinVisibility::True,this->vOutVelocity);
			//this->vOutVelocity->SetSubType2D(Double::MinValue,Double::MaxValue,0.01,0.0,0.0,false,false,false);
		}


		void GetBodyDetailsNode::Configurate(IPluginConfig^ Input)
		{

		}

		
		void GetBodyDetailsNode::Evaluate(int SpreadMax)
		{
			this->vInBodies->PinIsChanged;
			if (this->vInBodies->IsConnected) 
			{
				
				
				this->m_shapes->Reset();

				int cnt = this->vInBodies->SliceCount;
				this->vOutPosition->SliceCount = cnt;
				this->vOutVelocity->SliceCount = cnt;
				this->vOutRotation->SliceCount = cnt;
				this->vOutIsDynamic->SliceCount = cnt;
				this->vOutId->SliceCount = cnt;
				this->vOutCustom->SliceCount = cnt;
				this->vOutMass->SliceCount = cnt;
				this->vOutInertia->SliceCount = cnt;
				this->vOutShapeCount->SliceCount = cnt;
				this->vOutIsSleeping->SliceCount = cnt;
				this->vOutIsFrozen->SliceCount = cnt;
				List<String^>^ types = gcnew List<String^>();

				for (int i = 0; i < this->vInBodies->SliceCount; i++) 
				{
					b2Body* body;

					if (this->isbody) 
					{
						int realslicebody;
						this->vInBodies->GetUpsreamSlice(i % this->vInBodies->SliceCount,realslicebody);
						body = this->m_bodies->GetSlice(realslicebody);
					} 
					else
					{
						body = this->mGround->GetGround();
					}

					b2Vec2 pos = body->GetPosition();
					b2Vec2 vel = body->GetLinearVelocity();
					this->vOutPosition->SetValue2D(i, pos.x,pos.y);
					this->vOutVelocity->SetValue2D(i, vel.x,vel.y);
					this->vOutRotation->SetValue(i,body->GetAngle() / (Math::PI * 2.0));
					this->vOutIsDynamic->SetValue(i,Convert::ToInt32(body->IsDynamic()));
					this->vOutMass->SetValue(i,body->GetMass());
					this->vOutInertia->SetValue(i,body->GetInertia());
					this->vOutIsFrozen->SetValue(i,Convert::ToDouble(body->IsFrozen()));
					this->vOutIsSleeping->SetValue(i,Convert::ToDouble(body->IsSleeping()));

					if (this->isbody) 
					{
						BodyCustomData* bdata = (BodyCustomData*)body->GetUserData();
						this->vOutId->SetValue(i, bdata->Id);
						String^ str = gcnew String(bdata->Custom);
						this->vOutCustom->SetString(i,str);
					}
					else
					{
						this->vOutId->SetValue(i, -1);
						this->vOutCustom->SetString(i,"");
					}

					int shapecount = 0;
					for (b2Shape* s = body->GetShapeList(); s; s = s->GetNext())
					{
						if (s->GetType() == e_circleShape || s->GetType() == e_polygonShape || s->GetType() == e_edgeShape) 
						{
							this->m_shapes->Add(s);
							shapecount++;
						}

						String^ str;
						switch (s->GetType())
						{
						case e_circleShape:
							str = "Circle";
							break;
						case e_polygonShape:
							str = "Polygon";
							break;
						case e_edgeShape:
							str = "Edge";
							break;
						default:
							str = "Unknown";
							break;
						}
						types->Add(str);
						
					}
					this->vOutShapeCount->SetValue(i,shapecount);
				}

				this->vOutShapes->SliceCount = this->m_shapes->Count();
				this->vOutShapeType->SliceCount = types->Count;
				for (int i = 0; i < types->Count; i++)
				{
					this->vOutShapeType->SetString(i, types[i]);
				}
			}
		}


		void GetBodyDetailsNode::ConnectPin(IPluginIO^ Pin)
		{
			if (Pin == this->vInBodies)
        	{
				INodeIOBase^ usI;
				try 
				{
					this->vInBodies->GetUpstreamInterface(usI);
					this->m_bodies = (BodyDataType^)usI;
					this->isbody = true;
				} 
				catch (Exception^ ex)
				{
					this->vInBodies->GetUpstreamInterface(usI);
					this->mGround = (GroundDataType^)usI;
					this->isbody = false;
				}
        	}
		}

		void GetBodyDetailsNode::DisconnectPin(IPluginIO^ Pin)
		{
			if (Pin == this->vInBodies)
        	{
        		if (this->isbody) 
				{
        			this->m_bodies = nullptr;
				} 
				else 
				{
					this->mGround = nullptr;
				}
        	}
		}
	}
}
